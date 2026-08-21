import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CreditCard,
  RefreshCw,
  WalletCards,
  Plus,
  X,
} from 'lucide-react'
import { ApiError } from '../api/client'
import { getJobs } from '../api/jobs'
import {
  getPaymentSummary,
  recordPayment,
  type InvoicePaymentSummary,
  type PaymentMethod,
} from '../api/payments'
import './PaymentsPage.css'

interface PaymentsPageProps {
  tenantId: string
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

async function getPaymentSummaries(
  tenantId: string,
): Promise<InvoicePaymentSummary[]> {
  const jobs = await getJobs(tenantId)

  const results = await Promise.all(
    jobs.map(async (job) => {
      try {
        return await getPaymentSummary(
          tenantId,
          job.id,
        )
      } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
          return null
        }
        throw err
      }
    }),
  )

  return results.filter(
    (summary): summary is InvoicePaymentSummary =>
      summary !== null,
  )
}

function PaymentsPage({
  tenantId,
}: PaymentsPageProps) {
  const queryClient = useQueryClient()

  const [paymentOpen, setPaymentOpen] =
    useState(false)

  const [selectedInvoice, setSelectedInvoice] =
    useState<InvoicePaymentSummary | null>(null)

  const [amount, setAmount] = useState('')
  const [method, setMethod] =
    useState<PaymentMethod>('Cash')
  const [reference, setReference] = useState('')
  const [error, setError] = useState('')

  const paymentsQuery = useQuery({
    queryKey: ['payments', tenantId],
    queryFn: () => getPaymentSummaries(tenantId),
  })

  const summaries = paymentsQuery.data ?? []

  const totalCollected = summaries.reduce(
    (total, summary) =>
      total + summary.amountPaid,
    0,
  )

  const totalOutstanding = summaries.reduce(
    (total, summary) =>
      total + summary.balanceDue,
    0,
  )

  const paymentCount = summaries.reduce(
    (total, summary) =>
      total + summary.payments.length,
    0,
  )

  function openPayment(
    summary: InvoicePaymentSummary,
  ) {
    setSelectedInvoice(summary)
    setAmount('')
    setMethod('Cash')
    setReference('')
    setError('')
    setPaymentOpen(true)
  }

  function closePayment() {
    setPaymentOpen(false)
    setSelectedInvoice(null)
    setError('')
  }

  async function submitPayment(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!selectedInvoice) {
      return
    }

    const parsedAmount = Number(amount)

    if (
      !Number.isFinite(parsedAmount) ||
      parsedAmount <= 0
    ) {
      setError(
        'Payment amount must be greater than zero.',
      )
      return
    }

    if (
      parsedAmount >
      selectedInvoice.balanceDue
    ) {
      setError(
        'Payment cannot exceed the balance due.',
      )
      return
    }

    try {
      setError('')

      await recordPayment(
        tenantId,
        selectedInvoice.invoiceId,
        {
          amount: parsedAmount,
          method,
          reference:
            reference.trim() || undefined,
          paidAtUtc:
            new Date().toISOString(),
        },
      )

      await queryClient.invalidateQueries({
        queryKey: ['payments', tenantId],
      })

      closePayment()
    } catch (paymentError) {
      setError(
        paymentError instanceof Error
          ? paymentError.message
          : 'Unable to record payment.',
      )
    }
  }

  return (
    <main className="dashboard payments-workspace">
      <section className="payments-hero">
        <div>
          <p className="eyebrow">
            Finance workspace
          </p>

          <h1>Payments</h1>

          <p>
            Track collections, balances and
            customer payments.
          </p>
        </div>

        <button
          className="secondary-button"
          onClick={() => paymentsQuery.refetch()}
          type="button"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </section>

      <section className="payments-summary">
        <article>
          <WalletCards size={19} />

          <div>
            <span>Collected</span>

            <strong>
              {formatMoney(totalCollected)}
            </strong>
          </div>
        </article>

        <article>
          <CreditCard size={19} />

          <div>
            <span>Outstanding</span>

            <strong>
              {formatMoney(totalOutstanding)}
            </strong>
          </div>
        </article>

        <article>
          <WalletCards size={19} />

          <div>
            <span>Payments recorded</span>

            <strong>{paymentCount}</strong>
          </div>
        </article>
      </section>

      <section className="payments-panel">
        <header className="payments-panel-header">
          <div>
            <p className="eyebrow">
              Collections
            </p>

            <h2>Payment directory</h2>
          </div>

          <span>
            {paymentsQuery.isFetching
              ? 'Refreshing...'
              : `${summaries.length} invoices`}
          </span>
        </header>

        {paymentsQuery.isLoading ? (
          <div className="payments-state">
            <strong>
              Loading payments...
            </strong>

            <p>
              Fetching payment information from
              DispatchArc.
            </p>
          </div>
        ) : paymentsQuery.isError ? (
          <div className="payments-state">
            <WalletCards size={28} />

            <strong>
              Could not load payments
            </strong>

            <p>
              Check that the DispatchArc API is
              running and try again.
            </p>

            <button
              className="secondary-button"
              onClick={() =>
                paymentsQuery.refetch()
              }
              type="button"
            >
              Try again
            </button>
          </div>
        ) : summaries.length === 0 ? (
          <div className="payments-state">
            <WalletCards size={28} />

            <strong>
              No payment records yet
            </strong>

            <p>
              Payment information will appear
              once invoices have been created.
            </p>
          </div>
        ) : (
          <div className="payment-list">
            {summaries.map((summary) => (
              <article
                className="payment-row"
                key={summary.invoiceId}
              >
                <div className="payment-icon">
                  <WalletCards size={20} />
                </div>

                <div className="payment-main">
                  <strong>
                    {summary.invoiceNumber}
                  </strong>

                  <span>
                    {summary.payments.length}{' '}
                    payment
                    {summary.payments.length === 1
                      ? ''
                      : 's'}
                  </span>
                </div>

                <div className="payment-amount">
                  <span>Paid</span>

                  <strong>
                    {formatMoney(
                      summary.amountPaid,
                    )}
                  </strong>
                </div>

                <div className="payment-balance">
                  <span>Balance</span>

                  <strong>
                    {formatMoney(
                      summary.balanceDue,
                    )}
                  </strong>
                </div>

                <span
                  className={`payment-status payment-${summary.status.toLowerCase()}`}
                >
                  {summary.status}
                </span>

                {summary.balanceDue > 0 &&
                  summary.status !== 'Void' && (
                    <button
                      className="primary-button payment-action"
                      onClick={() =>
                        openPayment(summary)
                      }
                      type="button"
                    >
                      <Plus size={15} />
                      Record payment
                    </button>
                  )}
              </article>
            ))}
          </div>
        )}
      </section>

      {paymentOpen && selectedInvoice && (
        <div className="payment-modal-backdrop">
          <section
            aria-modal="true"
            aria-labelledby="payment-title"
            className="payment-modal"
            role="dialog"
          >
            <header>
              <div>
                <p className="eyebrow">
                  Payment entry
                </p>

                <h2 id="payment-title">
                  Record payment
                </h2>

                <span>
                  {selectedInvoice.invoiceNumber}
                </span>
              </div>

              <button
                aria-label="Close"
                className="job-modal-close"
                onClick={closePayment}
                type="button"
              >
                <X size={18} />
              </button>
            </header>

            <div className="payment-modal-balance">
              <span>Balance due</span>

              <strong>
                {formatMoney(
                  selectedInvoice.balanceDue,
                )}
              </strong>
            </div>

            <form onSubmit={submitPayment}>
              <label className="payment-form-field">
                <span>Amount</span>

                <input
                  min="0.01"
                  max={selectedInvoice.balanceDue}
                  onChange={(event) =>
                    setAmount(event.target.value)
                  }
                  placeholder="0.00"
                  required
                  step="0.01"
                  type="number"
                  value={amount}
                />
              </label>

              <label className="payment-form-field">
                <span>Payment method</span>

                <select
                  onChange={(event) =>
                    setMethod(
                      event.target.value as PaymentMethod,
                    )
                  }
                  value={method}
                >
                  <option value="Cash">Cash</option>
                  <option value="Card">Card</option>
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

              <label className="payment-form-field">
                <span>Reference</span>

                <input
                  onChange={(event) =>
                    setReference(event.target.value)
                  }
                  placeholder="Optional reference"
                  type="text"
                  value={reference}
                />
              </label>

              {error && (
                <div className="payment-form-error">
                  {error}
                </div>
              )}

              <footer>
                <button
                  className="secondary-button"
                  onClick={closePayment}
                  type="button"
                >
                  Cancel
                </button>

                <button
                  className="primary-button"
                  type="submit"
                >
                  <Plus size={16} />
                  Record payment
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}
    </main>
  )
}

export default PaymentsPage
