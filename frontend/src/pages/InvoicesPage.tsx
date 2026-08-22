import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  BriefcaseBusiness,
  CreditCard,
  FileText,
  RefreshCw,
  X,
} from 'lucide-react'
import { getJobs } from '../api/jobs'
import { ApiError } from '../api/client'
import {
  createInvoice,
  getInvoiceByJob,
  type Invoice,
} from '../api/invoices'
import {
  getPaymentSummary,
  recordPayment,
  type InvoicePaymentSummary,
  type PaymentMethod,
} from '../api/payments'
import './InvoicesPage.css'

interface InvoicesPageProps {
  tenantId: string
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

async function getInvoicesForJobs(
  tenantId: string,
  jobs: Awaited<ReturnType<typeof getJobs>>,
): Promise<Invoice[]> {
  const results = await Promise.all(
    jobs.map(async (job) => {
      try {
        return await getInvoiceByJob(tenantId, job.id)
      } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
          return null
        }
        throw err
      }
    }),
  )

  return results.filter(
    (invoice): invoice is Invoice => invoice !== null,
  )
}

function InvoicesPage({ tenantId }: InvoicesPageProps) {
  const queryClient = useQueryClient()

  const [selectedInvoice, setSelectedInvoice] =
    useState<Invoice | null>(null)

  const [paymentAmount, setPaymentAmount] = useState('')
  const [paymentMethod, setPaymentMethod] =
    useState<PaymentMethod>('Cash')
  const [paymentReference, setPaymentReference] =
    useState('')
  const [paymentError, setPaymentError] = useState('')
    const [invoiceError, setInvoiceError] = useState('')

  const invoicesQuery = useQuery({
    queryKey: ['invoices', tenantId],
    queryFn: async () => {
      const jobs = await getJobs(tenantId)

      return getInvoicesForJobs(tenantId, jobs)
    },
  })

  const invoices = invoicesQuery.data ?? []

    const completedJobsQuery = useQuery({
      queryKey: ['completed-jobs-for-invoicing', tenantId],
      queryFn: () => getJobs(tenantId, { status: 'Completed' }),
    })

    const createInvoiceMutation = useMutation({
      mutationFn: (jobId: string) => createInvoice(tenantId, jobId),
      onSuccess: async () => {
        setInvoiceError('')
        await queryClient.invalidateQueries({ queryKey: ['invoices', tenantId] })
        await queryClient.invalidateQueries({ queryKey: ['completed-jobs-for-invoicing', tenantId] })
      },
      onError: (error) => {
        setInvoiceError(
          error instanceof Error
            ? error.message
            : 'Unable to create invoice.',
        )
      },
    })

    const invoicedJobIds = new Set(invoices.map((invoice) => invoice.serviceJobId))
    const readyToInvoice = (completedJobsQuery.data ?? []).filter(
      (job) => !invoicedJobIds.has(job.id),
    )

  const paymentQuery = useQuery({
    queryKey: [
      'invoice-payment-summary',
      tenantId,
      selectedInvoice?.id,
    ],
    queryFn: () =>
      getPaymentSummary(
        tenantId,
        selectedInvoice!.id,
      ),
    enabled: Boolean(selectedInvoice),
  })

  const paymentMutation = useMutation({
    mutationFn: () => {
      if (!selectedInvoice) {
        throw new Error('No invoice selected.')
      }

      return recordPayment(
        tenantId,
        selectedInvoice.id,
        {
          amount: Number(paymentAmount),
          method: paymentMethod,
          reference:
            paymentReference.trim() || undefined,
        },
      )
    },

    onSuccess: async (
      summary: InvoicePaymentSummary,
    ) => {
      setPaymentAmount('')
      setPaymentReference('')
      setPaymentError('')

      await queryClient.invalidateQueries({
        queryKey: ['invoices', tenantId],
      })

      await queryClient.setQueryData(
        [
          'invoice-payment-summary',
          tenantId,
          selectedInvoice?.id,
        ],
        summary,
      )

      setSelectedInvoice((current) =>
        current
          ? {
              ...current,
              status: summary.status,
            }
          : current,
      )
    },

    onError: (error) => {
      setPaymentError(
        error instanceof Error
          ? error.message
          : 'Unable to record payment.',
      )
    },
  })

  const outstanding = invoices
    .filter(
      (invoice) =>
        invoice.status !== 'Paid' &&
        invoice.status !== 'Void',
    )
    .reduce(
      (total, invoice) => total + invoice.total,
      0,
    )

  const paidCount = invoices.filter(
    (invoice) => invoice.status === 'Paid',
  ).length

  function openInvoice(invoice: Invoice) {
    setSelectedInvoice(invoice)
    setPaymentAmount('')
    setPaymentReference('')
    setPaymentError('')
  }

  function closeInvoice() {
    if (paymentMutation.isPending) {
      return
    }

    setSelectedInvoice(null)
    setPaymentError('')
  }

  function submitPayment(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()
    setPaymentError('')

    const amount = Number(paymentAmount)

    if (!Number.isFinite(amount) || amount <= 0) {
      setPaymentError(
        'Enter a valid payment amount.',
      )
      return
    }

    if (
      paymentQuery.data &&
      amount > paymentQuery.data.balanceDue
    ) {
      setPaymentError(
        'Payment cannot exceed the balance due.',
      )
      return
    }

    paymentMutation.mutate()
  }

  return (
    <main className="dashboard invoices-workspace">
      <section className="invoices-hero">
        <div>
          <p className="eyebrow">
            Finance workspace
          </p>

          <h1>Invoices</h1>

          <p>
            Create, review and track customer
            invoices.
          </p>
        </div>

        <button
          className="secondary-button"
          onClick={() =>
            invoicesQuery.refetch()
          }
          type="button"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </section>

      <section className="invoices-summary">
        <article>
          <span>Total invoices</span>
          <strong>{invoices.length}</strong>
        </article>

        <article>
          <span>Outstanding</span>
          <strong>
            {formatMoney(outstanding)}
          </strong>
        </article>

        <article>
          <span>Paid</span>
          <strong>{paidCount}</strong>
        </article>
      </section>

      {(completedJobsQuery.isLoading || readyToInvoice.length > 0) && (
        <section className="invoices-ready-panel">
          <header>
            <div>
              <p className="eyebrow">Next step</p>
              <h2>Ready to invoice</h2>
            </div>
            <span>{readyToInvoice.length} completed jobs</span>
          </header>

          {completedJobsQuery.isLoading ? (
            <p className="invoices-ready-state">Checking completed jobs...</p>
          ) : (
            <div className="invoices-ready-list">
              {readyToInvoice.map((job) => (
                <div className="invoices-ready-row" key={job.id}>
                  <div className="invoices-ready-icon">
                    <BriefcaseBusiness size={17} />
                  </div>
                  <div className="invoices-ready-copy">
                    <strong>{job.jobNumber}</strong>
                    <span>{job.title}</span>
                  </div>
                  <button
                    className="primary-button"
                    disabled={createInvoiceMutation.isPending}
                    onClick={() => {
                      setInvoiceError('')
                      createInvoiceMutation.mutate(job.id)
                    }}
                    type="button"
                  >
                    <FileText size={14} />
                    Create invoice
                  </button>
                </div>
              ))}
            </div>
          )}

          {invoiceError && <div className="invoices-ready-error">{invoiceError}</div>}
        </section>
      )}

      <section className="invoices-panel">
        <header>
          <div>
            <p className="eyebrow">Billing</p>
            <h2>Invoice directory</h2>
          </div>

          <span>
            {invoicesQuery.isFetching
              ? 'Refreshing...'
              : `${invoices.length} invoices`}
          </span>
        </header>

        {invoicesQuery.isLoading ? (
          <div className="invoices-state">
            <strong>
              Loading invoices...
            </strong>

            <p>
              Fetching billing information from
              DispatchArc.
            </p>
          </div>
        ) : invoicesQuery.isError ? (
          <div className="invoices-state">
            <FileText size={28} />

            <strong>
              Could not load invoices
            </strong>

            <p>
              Check that the DispatchArc API is
              running and try again.
            </p>

            <button
              className="secondary-button"
              onClick={() =>
                invoicesQuery.refetch()
              }
              type="button"
            >
              Try again
            </button>
          </div>
        ) : invoices.length === 0 ? (
          <div className="invoices-state">
            <FileText size={28} />

            <strong>No invoices yet</strong>

            <p>
              Invoices will appear here once they
              are created for service jobs.
            </p>
          </div>
        ) : (
          <div className="invoice-list">
            {invoices.map((invoice) => (
              <button
                className="invoice-row"
                key={invoice.id}
                onClick={() =>
                  openInvoice(invoice)
                }
                type="button"
              >
                <div className="invoice-icon">
                  <FileText size={20} />
                </div>

                <div className="invoice-main">
                  <strong>
                    {invoice.invoiceNumber}
                  </strong>

                  <span>
                    Issued{' '}
                    {formatDate(
                      invoice.issuedAtUtc,
                    )}
                  </span>
                </div>

                <div className="invoice-due">
                  <span>Due</span>

                  <strong>
                    {formatDate(
                      invoice.dueAtUtc,
                    )}
                  </strong>
                </div>

                <div className="invoice-total">
                  <span>Total</span>

                  <strong>
                    {formatMoney(invoice.total)}
                  </strong>
                </div>

                <span
                  className={`invoice-status status-${invoice.status.toLowerCase()}`}
                >
                  {invoice.status}
                </span>
              </button>
            ))}
          </div>
        )}
      </section>

      {selectedInvoice && (
        <div
          className="invoice-modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeInvoice()
            }
          }}
        >
          <section
            aria-modal="true"
            className="invoice-modal"
            role="dialog"
          >
            <header className="invoice-modal-header">
              <div>
                <p className="eyebrow">
                  Invoice details
                </p>

                <h2>
                  {selectedInvoice.invoiceNumber}
                </h2>
              </div>

              <button
                aria-label="Close invoice"
                className="job-modal-close"
                onClick={closeInvoice}
                type="button"
              >
                <X size={18} />
              </button>
            </header>

            <div className="invoice-detail-grid">
              <div>
                <span>Invoice status</span>

                <strong>
                  {selectedInvoice.status}
                </strong>
              </div>

              <div>
                <span>Issued</span>

                <strong>
                  {formatDate(
                    selectedInvoice.issuedAtUtc,
                  )}
                </strong>
              </div>

              <div>
                <span>Due</span>

                <strong>
                  {formatDate(
                    selectedInvoice.dueAtUtc,
                  )}
                </strong>
              </div>

              <div>
                <span>Job</span>

                <strong>
                  {selectedInvoice.serviceJobId.slice(
                    0,
                    8,
                  )}
                  ...
                </strong>
              </div>
            </div>

            <section className="invoice-line-items">
              <div className="invoice-section-heading">
                <div>
                  <p className="eyebrow">
                    Charges
                  </p>

                  <h3>Line items</h3>
                </div>
              </div>

              {selectedInvoice.lineItems
                .length === 0 ? (
                <div className="invoice-empty-line-items">
                  No line items recorded.
                </div>
              ) : (
                selectedInvoice.lineItems.map(
                  (item) => (
                    <div
                      className="invoice-line-item"
                      key={item.id}
                    >
                      <div>
                        <strong>
                          {item.description}
                        </strong>

                        <span>
                          {item.quantity} ×{' '}
                          {formatMoney(
                            item.unitPrice,
                          )}
                        </span>
                      </div>

                      <strong>
                        {formatMoney(
                          item.lineTotal,
                        )}
                      </strong>
                    </div>
                  ),
                )
              )}

              <div className="invoice-total-breakdown">
                <span>Subtotal</span>

                <strong>
                  {formatMoney(
                    selectedInvoice.subtotal,
                  )}
                </strong>

                <span>Total</span>

                <strong>
                  {formatMoney(
                    selectedInvoice.total,
                  )}
                </strong>
              </div>
            </section>

            <section className="payment-section">
              <div className="invoice-section-heading">
                <div>
                  <p className="eyebrow">
                    Payments
                  </p>

                  <h3>Payment summary</h3>
                </div>
              </div>

              {paymentQuery.isLoading ? (
                <div className="payment-loading">
                  Loading payment information...
                </div>
              ) : paymentQuery.isError ? (
                <div className="payment-error">
                  Could not load payment
                  information.
                </div>
              ) : paymentQuery.data ? (
                <>
                  <div className="payment-summary">
                    <div>
                      <span>Invoice total</span>

                      <strong>
                        {formatMoney(
                          paymentQuery.data
                            .invoiceTotal,
                        )}
                      </strong>
                    </div>

                    <div>
                      <span>Paid</span>

                      <strong>
                        {formatMoney(
                          paymentQuery.data
                            .amountPaid,
                        )}
                      </strong>
                    </div>

                    <div>
                      <span>Balance due</span>

                      <strong>
                        {formatMoney(
                          paymentQuery.data
                            .balanceDue,
                        )}
                      </strong>
                    </div>
                  </div>

                  {paymentQuery.data.payments
                    .length > 0 && (
                    <div className="payment-history">
                      <h4>
                        Payment history
                      </h4>

                      {paymentQuery.data.payments.map(
                        (payment) => (
                          <div
                            className="payment-row"
                            key={payment.id}
                          >
                            <div>
                              <strong>
                                {payment.paymentNumber}
                              </strong>

                              <span>
                                {payment.method}
                                {' · '}
                                {formatDate(
                                  payment.paidAtUtc,
                                )}
                              </span>
                            </div>

                            <strong>
                              {formatMoney(
                                payment.amount,
                              )}
                            </strong>
                          </div>
                        ),
                      )}
                    </div>
                  )}

                  {paymentQuery.data.balanceDue >
                    0 && (
                    <form
                      className="payment-form"
                      onSubmit={submitPayment}
                    >
                      <div className="payment-form-title">
                        <CreditCard
                          size={17}
                        />

                        <strong>
                          Record payment
                        </strong>
                      </div>

                      <label>
                        <span>Amount</span>

                        <input
                          min="0.01"
                          onChange={(event) =>
                            setPaymentAmount(
                              event.target.value,
                            )
                          }
                          placeholder="0.00"
                          step="0.01"
                          type="number"
                          value={paymentAmount}
                        />
                      </label>

                      <label>
                        <span>Method</span>

                        <select
                          onChange={(event) =>
                            setPaymentMethod(
                              event.target
                                .value as PaymentMethod,
                            )
                          }
                          value={paymentMethod}
                        >
                          <option value="Cash">
                            Cash
                          </option>

                          <option value="Card">
                            Card
                          </option>

                          <option value="BankTransfer">
                            Bank transfer
                          </option>

                          <option value="Online">
                            Online
                          </option>

                          <option value="Other">
                            Other
                          </option>
                        </select>
                      </label>

                      <label>
                        <span>
                          Reference
                        </span>

                        <input
                          onChange={(event) =>
                            setPaymentReference(
                              event.target.value,
                            )
                          }
                          placeholder="Optional reference"
                          type="text"
                          value={paymentReference}
                        />
                      </label>

                      {paymentError && (
                        <div className="payment-form-error">
                          {paymentError}
                        </div>
                      )}

                      <button
                        className="primary-button"
                        disabled={
                          paymentMutation.isPending
                        }
                        type="submit"
                      >
                        <CreditCard
                          size={16}
                        />

                        {paymentMutation.isPending
                          ? 'Recording...'
                          : 'Record payment'}
                      </button>
                    </form>
                  )}
                </>
              ) : null}
            </section>

            <footer className="invoice-modal-footer">
              <button
                className="secondary-button"
                onClick={closeInvoice}
                type="button"
              >
                <ArrowLeft size={16} />
                Back to invoices
              </button>
            </footer>
          </section>
        </div>
      )}
    </main>
  )
}

export default InvoicesPage