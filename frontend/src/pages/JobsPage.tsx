import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BriefcaseBusiness,
  CalendarDays,
  ChevronRight,
  Clock3,
  FileText,
  MessageSquare,
  Plus,
  Search,
  Trash2,
  UserRound,
  X,
} from 'lucide-react'
import {
  addJobLineItem,
  deleteJobLineItem,
  getJobQuote,
} from '../api/quote'
import {
  addJobNote,
  getJobNotes,
  type JobNoteType,
} from '../api/notes'

import {
  approveJob,
  assignTechnician,
  cancelJob,
  completeJob,
  createJob,
  getJobById,
  getJobs,
  quoteJob,
  scheduleJob,
  startJob,
} from '../api/jobs'

import { getTechnicians } from '../api/technicians'

import { createCustomer, getCustomers } from '../api/customers'

import type {
  JobPriority,
  JobStatus,
} from '../api/types'

import './JobsPage.css'

interface JobsPageProps {
  tenantId: string
}

const statusOptions: Array<JobStatus | 'All'> = [
  'All',
  'New',
  'Quoted',
  'Approved',
  'Scheduled',
  'InProgress',
  'Completed',
  'Invoiced',
  'Cancelled',
]

const statusLabels: Record<JobStatus, string> = {
  New: 'New',
  Quoted: 'Quoted',
  Approved: 'Approved',
  Scheduled: 'Scheduled',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Invoiced: 'Invoiced',
  Cancelled: 'Cancelled',
}

function JobsPage({ tenantId }: JobsPageProps) {
  const queryClient = useQueryClient()

  const [status, setStatus] = useState<JobStatus | 'All'>('All')
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)

  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)

  const [selectedTechnicianId, setSelectedTechnicianId] = useState('')
  const [scheduleStart, setScheduleStart] = useState('')
  const [scheduleEnd, setScheduleEnd] = useState('')
  const [dispatchError, setDispatchError] = useState('')

  // -------------------------
  // JOB FORM
  // -------------------------

  const [customerId, setCustomerId] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] =
    useState<JobPriority>('Normal')
  const [createError, setCreateError] = useState('')

  // -------------------------
  // CUSTOMER FORM
  // -------------------------

  const [customerName, setCustomerName] = useState('')
  const [customerPhone, setCustomerPhone] = useState('')
  const [customerEmail, setCustomerEmail] = useState('')
  const [customerAddress, setCustomerAddress] = useState('')
  const [customerCity, setCustomerCity] = useState('')
  const [customerError, setCustomerError] = useState('')

  // -------------------------
  // JOBS
  // -------------------------

  const jobsQuery = useQuery({
    queryKey: ['jobs', tenantId, status, search],

    queryFn: () =>
      getJobs(tenantId, {
        status:
          status === 'All'
            ? undefined
            : status,
        search:
          search.trim() || undefined,
      }),
  })

  // -------------------------
  // CUSTOMERS
  // -------------------------

  const customersQuery = useQuery({
    queryKey: ['customers', tenantId],

    queryFn: () =>
      getCustomers(tenantId),
  })

  // -------------------------
  // TECHNICIANS
  // -------------------------

  const techniciansQuery = useQuery({
    queryKey: ['technicians', tenantId],

    queryFn: () =>
      getTechnicians(tenantId),
  })

  // -------------------------
  // SELECTED JOB
  // -------------------------

  const selectedJobQuery = useQuery({
    queryKey: ['job', tenantId, selectedJobId],
    queryFn: () => getJobById(tenantId, selectedJobId!),
    enabled: Boolean(selectedJobId),
  })

  // -------------------------
  // QUOTE & LINE ITEMS
  // -------------------------

  const [lineItemDesc, setLineItemDesc] = useState('')
  const [lineItemQty, setLineItemQty] = useState('1')
  const [lineItemPrice, setLineItemPrice] = useState('')
  const [lineItemError, setLineItemError] = useState('')

  // -------------------------
  // JOB NOTES
  // -------------------------

  const [noteContent, setNoteContent] = useState('')
  const [noteType, setNoteType] = useState<JobNoteType>('InternalNote')
  const [noteError, setNoteError] = useState('')

  // -------------------------
  // SELECTED JOB QUOTE & NOTES
  // -------------------------

  const quoteQuery = useQuery({
    queryKey: ['job-quote', tenantId, selectedJobId],
    queryFn: () => getJobQuote(tenantId, selectedJobId!),
    enabled: Boolean(selectedJobId),
  })

  const notesQuery = useQuery({
    queryKey: ['job-notes', tenantId, selectedJobId],
    queryFn: () => getJobNotes(tenantId, selectedJobId!),
    enabled: Boolean(selectedJobId),
  })

  const addLineItemMutation = useMutation({
    mutationFn: () => {
      const qty = parseFloat(lineItemQty)
      const price = parseFloat(lineItemPrice)
      if (!lineItemDesc.trim()) throw new Error('Description is required.')
      if (isNaN(qty) || qty <= 0) throw new Error('Quantity must be greater than 0.')
      if (isNaN(price) || price < 0) throw new Error('Unit price must be valid.')
      return addJobLineItem(tenantId, selectedJobId!, {
        description: lineItemDesc.trim(),
        quantity: qty,
        unitPrice: price,
      })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['job-quote', tenantId, selectedJobId] })
      setLineItemDesc('')
      setLineItemQty('1')
      setLineItemPrice('')
      setLineItemError('')
    },
    onError: (err) => {
      setLineItemError(err instanceof Error ? err.message : 'Unable to add line item.')
    },
  })

  const deleteLineItemMutation = useMutation({
    mutationFn: (lineItemId: string) => deleteJobLineItem(tenantId, selectedJobId!, lineItemId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['job-quote', tenantId, selectedJobId] })
    },
    onError: (err) => {
      setLineItemError(err instanceof Error ? err.message : 'Unable to delete line item.')
    },
  })

  const addNoteMutation = useMutation({
    mutationFn: () => {
      if (!noteContent.trim()) throw new Error('Note content cannot be empty.')
      return addJobNote(tenantId, selectedJobId!, {
        type: noteType,
        content: noteContent.trim(),
      })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['job-notes', tenantId, selectedJobId] })
      setNoteContent('')
      setNoteError('')
    },
    onError: (err) => {
      setNoteError(err instanceof Error ? err.message : 'Unable to add note.')
    },
  })

  // -------------------------
  // CREATE JOB
  // -------------------------

  const createJobMutation = useMutation({
    mutationFn: () =>
      createJob(tenantId, {
        customerId,
        title: title.trim(),
        description:
          description.trim(),
        priority,
      }),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['jobs', tenantId],
      })

      setCreateOpen(false)
      setCustomerId('')
      setTitle('')
      setDescription('')
      setPriority('Normal')
      setCreateError('')
    },

    onError: (error) => {
      setCreateError(
        error instanceof Error
          ? error.message
          : 'Unable to create the job.',
      )
    },
  })

  // -------------------------
  // CREATE CUSTOMER
  // -------------------------

  const createCustomerMutation =
    useMutation({
      mutationFn: () =>
        createCustomer(tenantId, {
          name:
            customerName.trim(),

          phone:
            customerPhone.trim(),

          email:
            customerEmail.trim() ||
            null,

          addressLine:
            customerAddress.trim() ||
            null,

          city:
            customerCity.trim() ||
            null,
        }),

      onSuccess: async (data) => {
        await queryClient.invalidateQueries({
          queryKey: [
            'customers',
            tenantId,
          ],
        })

        setCustomerId(data.id)

        setCustomerName('')
        setCustomerPhone('')
        setCustomerEmail('')
        setCustomerAddress('')
        setCustomerCity('')
        setCustomerError('')
      },

      onError: (error) => {
        setCustomerError(
          error instanceof Error
            ? error.message
            : 'Unable to create the customer.',
        )
      },
    })

  // -------------------------
  // WORKFLOW
  // -------------------------

  const workflowMutation =
    useMutation({
      mutationFn: async ({
        action,
        jobId,
      }: {
        action:
          | 'quote'
          | 'approve'
          | 'start'
          | 'complete'
          | 'cancel'

        jobId: string
      }) => {
        switch (action) {
          case 'quote':
            return quoteJob(
              tenantId,
              jobId,
            )

          case 'approve':
            return approveJob(
              tenantId,
              jobId,
            )

          case 'start':
            return startJob(
              tenantId,
              jobId,
            )

          case 'complete':
            return completeJob(
              tenantId,
              jobId,
            )

          case 'cancel':
            return cancelJob(
              tenantId,
              jobId,
            )
        }
      },

      onSuccess: async (
        updatedJob,
      ) => {
        await queryClient.invalidateQueries({
          queryKey: [
            'jobs',
            tenantId,
          ],
        })

        await queryClient.invalidateQueries({
          queryKey: [
            'job',
            tenantId,
            updatedJob.id,
          ],
        })
      },

      onError: (error) => {
        setDispatchError(
          error instanceof Error
            ? error.message
            : 'Unable to update job.',
        )
      },
    })

  // -------------------------
  // ASSIGN TECHNICIAN
  // -------------------------

  const assignTechnicianMutation =
    useMutation({
      mutationFn: () => {
        if (
          !selectedJobId ||
          !selectedTechnicianId
        ) {
          throw new Error(
            'Please select a technician.',
          )
        }

        return assignTechnician(
          tenantId,
          selectedJobId,
          selectedTechnicianId,
        )
      },

      onSuccess: async (
        updatedJob,
      ) => {
        setDispatchError('')
        setSelectedTechnicianId('')

        await queryClient.invalidateQueries({
          queryKey: [
            'jobs',
            tenantId,
          ],
        })

        await queryClient.invalidateQueries({
          queryKey: [
            'job',
            tenantId,
            updatedJob.id,
          ],
        })
      },

      onError: (error) => {
        setDispatchError(
          error instanceof Error
            ? error.message
            : 'Unable to assign technician.',
        )
      },
    })

  // -------------------------
  // SCHEDULE JOB
  // -------------------------

  const scheduleMutation =
    useMutation({
      mutationFn: () => {
        if (!selectedJobId) {
          throw new Error(
            'No job selected.',
          )
        }

        if (
          !scheduleStart ||
          !scheduleEnd
        ) {
          throw new Error(
            'Start and end times are required.',
          )
        }

        return scheduleJob(
          tenantId,
          selectedJobId,
          {
            startUtc:
              new Date(
                scheduleStart,
              ).toISOString(),

            endUtc:
              new Date(
                scheduleEnd,
              ).toISOString(),
          },
        )
      },

      onSuccess: async (
        updatedJob,
      ) => {
        setDispatchError('')

        await queryClient.invalidateQueries({
          queryKey: [
            'jobs',
            tenantId,
          ],
        })

        await queryClient.invalidateQueries({
          queryKey: [
            'job',
            tenantId,
            updatedJob.id,
          ],
        })

        setScheduleStart('')
        setScheduleEnd('')
      },

      onError: (error) => {
        setDispatchError(
          error instanceof Error
            ? error.message
            : 'Unable to schedule job.',
        )
      },
    })

  // -------------------------
  // CUSTOMER MAP
  // -------------------------

  const customerNames =
    useMemo(() => {
      return new Map(
        (
          customersQuery.data ??
          []
        ).map((customer) => [
          customer.id,
          customer.name,
        ]),
      )
    }, [customersQuery.data])

  const customers =
    customersQuery.data ?? []

  const technicians =
    techniciansQuery.data ?? []

  const jobs =
    jobsQuery.data ?? []

  // -------------------------
  // OPEN CREATE
  // -------------------------

  function openCreateModal() {
    setCreateError('')
    setCustomerError('')

    setCustomerName('')
    setCustomerPhone('')
    setCustomerEmail('')
    setCustomerAddress('')
    setCustomerCity('')

    if (
      !customerId &&
      customers.length > 0
    ) {
      setCustomerId(
        customers[0].id,
      )
    }

    setCreateOpen(true)
  }

  // -------------------------
  // CREATE JOB
  // -------------------------

  function submitNewJob(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    setCreateError('')

    if (
      !customerId ||
      !title.trim()
    ) {
      setCreateError(
        'Customer and job title are required.',
      )

      return
    }

    createJobMutation.mutate()
  }

  // -------------------------
  // CREATE CUSTOMER
  // -------------------------

  function submitNewCustomer(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    setCustomerError('')

    if (
      !customerName.trim() ||
      !customerPhone.trim()
    ) {
      setCustomerError(
        'Customer name and phone are required.',
      )

      return
    }

    createCustomerMutation.mutate()
  }

  // -------------------------
  // CLOSE CREATE
  // -------------------------

  function closeCreateModal() {
    if (
      createJobMutation.isPending ||
      createCustomerMutation.isPending
    ) {
      return
    }

    setCreateOpen(false)
  }

  // -------------------------
  // OPEN JOB
  // -------------------------

  function openJob(jobId: string) {
    setDispatchError('')
    setSelectedTechnicianId('')
    setScheduleStart('')
    setScheduleEnd('')
    setSelectedJobId(jobId)
  }

  // -------------------------
  // CLOSE DRAWER
  // -------------------------

  function closeDetailsDrawer() {
    if (
      workflowMutation.isPending ||
      assignTechnicianMutation.isPending ||
      scheduleMutation.isPending
    ) {
      return
    }

    setSelectedJobId(null)
    setDispatchError('')
    setSelectedTechnicianId('')
    setScheduleStart('')
    setScheduleEnd('')
  }

  return (
    <main className="dashboard jobs-workspace">

      {/* HERO */}

      <section className="jobs-hero">
        <div>
          <p className="eyebrow">
            Dispatch workspace
          </p>

          <h1>Jobs</h1>

          <p className="hero-copy">
            Track every service job from intake through completion.
          </p>
        </div>

        <button
          className="primary-button"
          onClick={openCreateModal}
          type="button"
        >
          <Plus size={17} />
          New job
        </button>
      </section>

      {/* TOOLBAR */}

      <section className="jobs-toolbar">

        <div className="jobs-search">
          <Search size={17} />

          <input
            onChange={(event) =>
              setSearch(
                event.target.value,
              )
            }
            placeholder="Search job number, title or description..."
            type="search"
            value={search}
          />
        </div>

        <div className="jobs-status-filter">
          {statusOptions.map(
            (option) => (
              <button
                className={
                  status === option
                    ? 'active'
                    : ''
                }
                key={option}
                onClick={() =>
                  setStatus(option)
                }
                type="button"
              >
                {option ===
                'InProgress'
                  ? 'In Progress'
                  : option}
              </button>
            ),
          )}
        </div>

      </section>

      {/* SUMMARY */}

      <section className="jobs-summary-grid">

        <article>
          <BriefcaseBusiness
            size={18}
          />

          <div>
            <span>Showing</span>
            <strong>
              {jobs.length}
            </strong>
          </div>
        </article>

        <article>
          <Clock3 size={18} />

          <div>
            <span>Filter</span>

            <strong>
              {status === 'All'
                ? 'All jobs'
                : status ===
                    'InProgress'
                  ? 'In Progress'
                  : status}
            </strong>
          </div>
        </article>

        <article>
          <CalendarDays
            size={18}
          />

          <div>
            <span>Workspace</span>
            <strong>
              Live API
            </strong>
          </div>
        </article>

      </section>

      {/* JOB LIST */}

      <section className="jobs-table-panel">

        <header className="jobs-table-header">
          <div>
            <p className="eyebrow">
              Service jobs
            </p>

            <h2>Job queue</h2>
          </div>

          <span>
            {jobsQuery.isFetching
              ? 'Refreshing...'
              : `${jobs.length} results`}
          </span>
        </header>

        {jobsQuery.isLoading ? (
          <div className="jobs-state">

            <div className="jobs-spinner" />

            <strong>
              Loading jobs
            </strong>

            <p>
              Fetching the latest dispatch data.
            </p>

          </div>
        ) : jobsQuery.isError ? (
          <div className="jobs-state jobs-state-error">

            <BriefcaseBusiness
              size={25}
            />

            <strong>
              Could not load jobs
            </strong>

            <p>
              Check that the DispatchArc API is running and try again.
            </p>

            <button
              className="secondary-button"
              onClick={() =>
                jobsQuery.refetch()
              }
              type="button"
            >
              Try again
            </button>

          </div>
        ) : jobs.length === 0 ? (
          <div className="jobs-state">

            <BriefcaseBusiness
              size={25}
            />

            <strong>
              No jobs found
            </strong>

            <p>
              There are no jobs matching the current search and status filter.
            </p>

          </div>
        ) : (
          <div className="jobs-list">

            {jobs.map((job) => {

              const customer =
                customerNames.get(
                  job.customerId,
                ) ??
                'Customer'

              const scheduled =
                job.scheduledStartUtc
                  ? new Date(
                      job.scheduledStartUtc,
                    ).toLocaleString(
                      [],
                      {
                        month:
                          'short',
                        day:
                          'numeric',
                        hour:
                          '2-digit',
                        minute:
                          '2-digit',
                      },
                    )
                  : 'Not scheduled'

              return (
                <article
                  className="jobs-list-row"
                  key={job.id}
                >

                  <div className="jobs-primary">

                    <span className="job-number">
                      {job.jobNumber}
                    </span>

                    <strong>
                      {job.title}
                    </strong>

                    <small>
                      {customer}
                    </small>

                  </div>

                  <div className="jobs-meta">

                    <span
                      className={`priority-badge priority-${job.priority.toLowerCase()}`}
                    >
                      {job.priority}
                    </span>

                    <span
                      className={`status-pill status-${job.status.toLowerCase()}`}
                    >
                      {
                        statusLabels[
                          job.status
                        ]
                      }
                    </span>

                  </div>

                  <div className="jobs-schedule">

                    <CalendarDays
                      size={15}
                    />

                    <span>
                      {scheduled}
                    </span>

                  </div>

                  <button
                    aria-label={`Open ${job.jobNumber}`}
                    className="jobs-open-button"
                    onClick={() =>
                      openJob(job.id)
                    }
                    type="button"
                  >
                    <ChevronRight
                      size={18}
                    />
                  </button>

                </article>
              )
            })}

          </div>
        )}

      </section>

      {/* CREATE JOB */}

      {createOpen && (
        <div className="job-modal-backdrop">

          <section
            aria-labelledby="new-job-title"
            aria-modal="true"
            className="job-modal"
            role="dialog"
          >

            <header className="job-modal-header">

              <div>
                <p className="eyebrow">
                  Dispatch intake
                </p>

                <h2 id="new-job-title">
                  Create new job
                </h2>
              </div>

              <button
                aria-label="Close new job"
                className="job-modal-close"
                onClick={
                  closeCreateModal
                }
                type="button"
              >
                <X size={18} />
              </button>

            </header>

            <form
              onSubmit={
                customers.length ===
                0
                  ? submitNewCustomer
                  : submitNewJob
              }
            >

              {customers.length ===
                0 &&
              !customersQuery.isLoading ? (
                <>
                  <div className="first-customer-intro">

                    <div>
                      <h3>
                        Add your first customer
                      </h3>

                      <p>
                        No customers exist in this workspace yet. A customer must exist before a service job can be created.
                      </p>
                    </div>

                  </div>

                  <div className="quick-customer">

                    <div className="quick-customer-heading">

                      <div>
                        <span>
                          Workspace Setup
                        </span>

                        <strong>
                          New Customer
                        </strong>
                      </div>

                      <p>
                        Create a customer profile to assign this job to.
                      </p>

                    </div>

                    <label className="job-form-field">
                      <span>
                        Customer name
                      </span>

                      <input
                        onChange={(event) =>
                          setCustomerName(
                            event.target
                              .value,
                          )
                        }
                        placeholder="e.g. Atlas Retail Group"
                        required
                        type="text"
                        value={
                          customerName
                        }
                      />
                    </label>

                    <label className="job-form-field">
                      <span>
                        Phone
                      </span>

                      <input
                        onChange={(event) =>
                          setCustomerPhone(
                            event.target
                              .value,
                          )
                        }
                        placeholder="e.g. 555-0199"
                        required
                        type="tel"
                        value={
                          customerPhone
                        }
                      />
                    </label>

                    <label className="job-form-field">
                      <span>
                        Email (Optional)
                      </span>

                      <input
                        onChange={(event) =>
                          setCustomerEmail(
                            event.target
                              .value,
                          )
                        }
                        placeholder="e.g. contact@atlas.com"
                        type="email"
                        value={
                          customerEmail
                        }
                      />
                    </label>

                    <label className="job-form-field">
                      <span>
                        Address (Optional)
                      </span>

                      <input
                        onChange={(event) =>
                          setCustomerAddress(
                            event.target
                              .value,
                          )
                        }
                        placeholder="e.g. 100 Main St"
                        type="text"
                        value={
                          customerAddress
                        }
                      />
                    </label>

                    <label className="job-form-field">
                      <span>
                        City (Optional)
                      </span>

                      <input
                        onChange={(event) =>
                          setCustomerCity(
                            event.target
                              .value,
                          )
                        }
                        placeholder="e.g. Seattle"
                        type="text"
                        value={
                          customerCity
                        }
                      />
                    </label>

                    {customerError && (
                      <div className="job-form-error">
                        {customerError}
                      </div>
                    )}

                    <button
                      className="primary-button quick-customer-button"
                      disabled={
                        createCustomerMutation.isPending
                      }
                      type="submit"
                    >
                      <Plus size={16} />

                      {createCustomerMutation.isPending
                        ? 'Creating...'
                        : 'Create customer'}
                    </button>

                  </div>

                  <footer className="job-modal-actions">

                    <button
                      className="secondary-button"
                      onClick={
                        closeCreateModal
                      }
                      type="button"
                    >
                      Cancel
                    </button>

                  </footer>
                </>
              ) : (
                <>
                  <label className="job-form-field">

                    <span>
                      Customer
                    </span>

                    <select
                      disabled={
                        customersQuery.isLoading
                      }
                      onChange={(event) =>
                        setCustomerId(
                          event.target
                            .value,
                        )
                      }
                      required
                      value={
                        customerId
                      }
                    >

                      <option value="">
                        {customersQuery.isLoading
                          ? 'Loading customers...'
                          : 'Select customer'}
                      </option>

                      {customers.map(
                        (customer) => (
                          <option
                            key={
                              customer.id
                            }
                            value={
                              customer.id
                            }
                          >
                            {
                              customer.name
                            }
                          </option>
                        ),
                      )}

                    </select>

                  </label>

                  <label className="job-form-field">

                    <span>
                      Job title
                    </span>

                    <input
                      onChange={(event) =>
                        setTitle(
                          event.target
                            .value,
                        )
                      }
                      placeholder="e.g. HVAC inspection and repair"
                      required
                      type="text"
                      value={title}
                    />

                  </label>

                  <label className="job-form-field">

                    <span>
                      Description
                    </span>

                    <textarea
                      onChange={(event) =>
                        setDescription(
                          event.target
                            .value,
                        )
                      }
                      placeholder="Add service details, symptoms or instructions..."
                      rows={4}
                      value={
                        description
                      }
                    />

                  </label>

                  <label className="job-form-field">

                    <span>
                      Priority
                    </span>

                    <select
                      onChange={(event) =>
                        setPriority(
                          event.target
                            .value as JobPriority,
                        )
                      }
                      value={
                        priority
                      }
                    >

                      <option value="Low">
                        Low
                      </option>

                      <option value="Normal">
                        Normal
                      </option>

                      <option value="High">
                        High
                      </option>

                      <option value="Urgent">
                        Urgent
                      </option>

                    </select>

                  </label>

                  {createError && (
                    <div className="job-form-error">
                      {createError}
                    </div>
                  )}

                  <footer className="job-modal-actions">

                    <button
                      className="secondary-button"
                      onClick={
                        closeCreateModal
                      }
                      type="button"
                    >
                      Cancel
                    </button>

                    <button
                      className="primary-button"
                      disabled={
                        createJobMutation.isPending ||
                        customers.length === 0
                      }
                      type="submit"
                    >
                      <Plus size={16} />

                      {createJobMutation.isPending
                        ? 'Creating...'
                        : 'Create job'}
                    </button>

                  </footer>
                </>
              )}

            </form>

          </section>

        </div>
      )}

      {/* JOB DETAILS DRAWER */}

      {selectedJobId && (
        <aside className="job-details-drawer">

          <header className="job-details-header">

            <div>
              <p className="eyebrow">
                Service job
              </p>

              <h2>
                Job details
              </h2>
            </div>

            <button
              aria-label="Close job details"
              className="job-modal-close"
              onClick={
                closeDetailsDrawer
              }
              type="button"
            >
              <X size={18} />
            </button>

          </header>

          <div className="job-details-content">

            {selectedJobQuery.isLoading ? (

              <div className="jobs-state">

                <div className="jobs-spinner" />

                <strong>
                  Loading job
                </strong>

                <p>
                  Fetching job details.
                </p>

              </div>

            ) : selectedJobQuery.isError ? (

              <div className="jobs-state jobs-state-error">

                <BriefcaseBusiness
                  size={25}
                />

                <strong>
                  Could not load job
                </strong>

                <p>
                  Check that the DispatchArc API is running and try again.
                </p>

                <button
                  className="secondary-button"
                  onClick={() =>
                    selectedJobQuery.refetch()
                  }
                  type="button"
                >
                  Try again
                </button>

              </div>

            ) : selectedJobQuery.data ? (

              <>

                {/* IDENTITY */}

                <div className="job-details-identity">

                  <span className="job-number">
                    {
                      selectedJobQuery
                        .data.jobNumber
                    }
                  </span>

                  <h3>
                    {
                      selectedJobQuery
                        .data.title
                    }
                  </h3>

                  <div className="job-details-badges">

                    <span
                      className={`priority-badge priority-${selectedJobQuery.data.priority.toLowerCase()}`}
                    >
                      {
                        selectedJobQuery
                          .data.priority
                      }
                    </span>

                    <span
                      className={`status-pill status-${selectedJobQuery.data.status.toLowerCase()}`}
                    >
                      {
                        statusLabels[
                          selectedJobQuery
                            .data.status
                        ]
                      }
                    </span>

                  </div>

                </div>

                {/* DESCRIPTION */}

                <div className="job-details-section">

                  <span>
                    DESCRIPTION
                  </span>

                  <p>
                    {
                      selectedJobQuery
                        .data
                        .description ||
                      'No description provided.'
                    }
                  </p>

                </div>

                {/* DETAILS */}

                <div className="job-details-grid">

                  <div>
                    <span>
                      CUSTOMER
                    </span>

                    <strong>
                      {
                        customerNames.get(
                          selectedJobQuery
                            .data
                            .customerId,
                        ) ??
                        'Customer'
                      }
                    </strong>
                  </div>

                  <div>
                    <span>
                      ASSIGNED TECHNICIAN
                    </span>

                    <strong>
                      {
                        selectedJobQuery
                          .data
                          .assignedTechnicianId
                          ? technicians.find(
                              (tech) =>
                                tech.id ===
                                selectedJobQuery
                                  .data!
                                  .assignedTechnicianId,
                            )?.fullName ??
                            'Assigned'
                          : 'Not assigned'
                      }
                    </strong>
                  </div>

                  <div>
                    <span>
                      SCHEDULE
                    </span>

                    <strong>
                      {
                        selectedJobQuery
                          .data
                          .scheduledStartUtc
                          ? new Date(
                              selectedJobQuery
                                .data
                                .scheduledStartUtc,
                            ).toLocaleString(
                              [],
                              {
                                month:
                                  'short',
                                day:
                                  'numeric',
                                hour:
                                  '2-digit',
                                minute:
                                  '2-digit',
                              },
                            )
                          : 'Not scheduled'
                      }
                    </strong>
                  </div>

                  <div>
                    <span>
                      CREATED
                    </span>

                    <strong>
                      {new Date(
                        selectedJobQuery
                          .data
                          .createdAtUtc,
                      ).toLocaleDateString(
                        [],
                        {
                          month:
                            'short',
                          day:
                            'numeric',
                          year:
                            'numeric',
                        },
                      )}
                    </strong>
                  </div>

                </div>

                {/* DISPATCH */}

                <div className="job-details-section">

                  <span>
                    DISPATCH
                  </span>

                  <div
                    style={{
                      display:
                        'flex',
                      flexDirection:
                        'column',
                      gap: '12px',
                      marginTop:
                        '10px',
                    }}
                  >

                    <label className="job-form-field">

                      <span>
                        Technician
                      </span>

                      <select
                        disabled={
                          techniciansQuery.isLoading ||
                          assignTechnicianMutation.isPending
                        }
                        onChange={(
                          event,
                        ) =>
                          setSelectedTechnicianId(
                            event.target
                              .value,
                          )
                        }
                        value={
                          selectedTechnicianId
                        }
                      >

                        <option value="">
                          {techniciansQuery.isLoading
                            ? 'Loading technicians...'
                            : technicians.length ===
                                0
                              ? 'No technicians available'
                              : 'Select technician'}
                        </option>

                        {technicians.map(
                          (
                            technician,
                          ) => (
                            <option
                              key={
                                technician.id
                              }
                              value={
                                technician.id
                              }
                            >
                              {
                                technician.fullName
                              }
                              {' — '}
                              {
                                technician.email
                              }
                            </option>
                          ),
                        )}

                      </select>

                    </label>

                    <button
                      className="primary-button"
                      disabled={
                        !selectedTechnicianId ||
                        assignTechnicianMutation.isPending
                      }
                      onClick={() =>
                        assignTechnicianMutation.mutate()
                      }
                      type="button"
                    >

                      <UserRound
                        size={16}
                      />

                      {assignTechnicianMutation.isPending
                        ? 'Assigning...'
                        : 'Assign technician'}

                    </button>

                  </div>

                </div>

                {/* SCHEDULING */}

                <div className="job-details-section">

                  <span>
                    SCHEDULE JOB
                  </span>

                  <div
                    style={{
                      display:
                        'flex',
                      flexDirection:
                        'column',
                      gap: '12px',
                      marginTop:
                        '10px',
                    }}
                  >

                    <label className="job-form-field">

                      <span>
                        Start
                      </span>

                      <input
                        type="datetime-local"
                        onChange={(
                          event,
                        ) =>
                          setScheduleStart(
                            event.target
                              .value,
                          )
                        }
                        value={
                          scheduleStart
                        }
                      />

                    </label>

                    <label className="job-form-field">

                      <span>
                        End
                      </span>

                      <input
                        type="datetime-local"
                        onChange={(
                          event,
                        ) =>
                          setScheduleEnd(
                            event.target
                              .value,
                          )
                        }
                        value={
                          scheduleEnd
                        }
                      />

                    </label>

                    <button
                      className="primary-button"
                      disabled={
                        !selectedJobQuery
                          .data
                          .assignedTechnicianId ||
                        !scheduleStart ||
                        !scheduleEnd ||
                        scheduleMutation.isPending
                      }
                      onClick={() =>
                        scheduleMutation.mutate()
                      }
                      type="button"
                    >

                      <CalendarDays
                        size={16}
                      />

                      {scheduleMutation.isPending
                        ? 'Scheduling...'
                        : 'Schedule job'}

                    </button>

                  </div>

                </div>

                {/* QUOTE LINE ITEMS */}
                <div className="job-quote-section">
                  <div className="job-quote-header">
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <FileText size={16} />
                      <span>Quote & Pricing</span>
                    </div>
                    {quoteQuery.data && (
                      <span className="job-quote-total-badge">
                        Total: {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(quoteQuery.data.subtotal)}
                      </span>
                    )}
                  </div>

                  {quoteQuery.isLoading ? (
                    <p style={{ color: 'var(--muted)', fontSize: '12px' }}>Loading quote...</p>
                  ) : (
                    <div className="job-line-items-list">
                      {quoteQuery.data?.lineItems.map((item) => (
                        <div className="job-line-item-card" key={item.id}>
                          <span className="job-line-item-desc" title={item.description}>{item.description}</span>
                          <span className="job-line-item-qty-price">{item.quantity} &times; ${item.unitPrice.toFixed(2)}</span>
                          <span className="job-line-item-total">${item.lineTotal.toFixed(2)}</span>
                          {selectedJobQuery.data.status === 'New' || selectedJobQuery.data.status === 'Quoted' ? (
                            <button
                              className="job-line-item-delete"
                              disabled={deleteLineItemMutation.isPending}
                              onClick={() => deleteLineItemMutation.mutate(item.id)}
                              title="Remove item"
                              type="button"
                            >
                              <Trash2 size={14} />
                            </button>
                          ) : null}
                        </div>
                      ))}

                      {quoteQuery.data?.lineItems.length === 0 && (
                        <p style={{ color: 'var(--muted)', fontSize: '12px', margin: '4px 0' }}>No line items added yet.</p>
                      )}
                    </div>
                  )}

                  {(selectedJobQuery.data.status === 'New' || selectedJobQuery.data.status === 'Quoted') && (
                    <form
                      className="job-add-item-form"
                      onSubmit={(e) => {
                        e.preventDefault()
                        addLineItemMutation.mutate()
                      }}
                    >
                      <input
                        onChange={(e) => setLineItemDesc(e.target.value)}
                        placeholder="Item description (e.g. Labor)"
                        required
                        type="text"
                        value={lineItemDesc}
                      />
                      <input
                        min="0.01"
                        onChange={(e) => setLineItemQty(e.target.value)}
                        placeholder="Qty"
                        required
                        step="any"
                        type="number"
                        value={lineItemQty}
                      />
                      <input
                        min="0"
                        onChange={(e) => setLineItemPrice(e.target.value)}
                        placeholder="Price"
                        required
                        step="0.01"
                        type="number"
                        value={lineItemPrice}
                      />
                      <button
                        className="primary-button"
                        disabled={addLineItemMutation.isPending}
                        type="submit"
                      >
                        <Plus size={14} />
                        Add
                      </button>
                    </form>
                  )}

                  {lineItemError && (
                    <div className="job-form-error">{lineItemError}</div>
                  )}
                </div>

                {/* JOB NOTES */}
                <div className="job-notes-section">
                  <div className="job-notes-header">
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <MessageSquare size={16} />
                      <span>Activity & Notes</span>
                    </div>
                    {notesQuery.data && (
                      <span style={{ fontSize: '11px', color: 'var(--muted)' }}>
                        {notesQuery.data.length} note{notesQuery.data.length === 1 ? '' : 's'}
                      </span>
                    )}
                  </div>

                  {notesQuery.isLoading ? (
                    <p style={{ color: 'var(--muted)', fontSize: '12px' }}>Loading notes...</p>
                  ) : (
                    <div className="job-notes-list">
                      {notesQuery.data?.map((note) => (
                        <div className="job-note-card" key={note.id}>
                          <div className="job-note-header">
                            <span className="job-note-author">{note.authorFullName}</span>
                            <span className={`job-note-type-pill ${note.type === 'TechnicianUpdate' ? 'note-technician' : 'note-internal'}`}>
                              {note.type === 'TechnicianUpdate' ? 'Technician' : 'Internal'}
                            </span>
                            <span className="job-note-time">
                              {new Date(note.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </span>
                          </div>
                          <p className="job-note-content">{note.content}</p>
                        </div>
                      ))}

                      {notesQuery.data?.length === 0 && (
                        <p style={{ color: 'var(--muted)', fontSize: '12px', margin: '4px 0' }}>No notes posted yet.</p>
                      )}
                    </div>
                  )}

                  <form
                    className="job-add-note-form"
                    onSubmit={(e) => {
                      e.preventDefault()
                      addNoteMutation.mutate()
                    }}
                  >
                    <div className="job-add-note-row">
                      <select
                        onChange={(e) => setNoteType(e.target.value as JobNoteType)}
                        value={noteType}
                      >
                        <option value="InternalNote">Internal Note</option>
                        <option value="TechnicianUpdate">Technician Update</option>
                      </select>
                      <button
                        className="primary-button"
                        disabled={addNoteMutation.isPending}
                        type="submit"
                      >
                        <Plus size={14} />
                        Post note
                      </button>
                    </div>
                    <textarea
                      onChange={(e) => setNoteContent(e.target.value)}
                      placeholder="Add a progress update, customer instruction, or internal comment..."
                      required
                      value={noteContent}
                    />
                  </form>

                  {noteError && (
                    <div className="job-form-error">{noteError}</div>
                  )}
                </div>

                {/* ERROR */}

                {dispatchError && (
                  <div className="job-form-error">
                    {dispatchError}
                  </div>
                )}

                {/* WORKFLOW */}

                <div className="job-details-actions">

                  {selectedJobQuery
                    .data.status ===
                    'New' && (
                    <button
                      className="primary-button"
                      disabled={
                        workflowMutation.isPending
                      }
                      onClick={() =>
                        workflowMutation.mutate(
                          {
                            action:
                              'quote',
                            jobId:
                              selectedJobQuery
                                .data!
                                .id,
                          },
                        )
                      }
                      type="button"
                    >
                      {workflowMutation.isPending
                        ? 'Updating...'
                        : 'Mark as quoted'}
                    </button>
                  )}

                  {selectedJobQuery
                    .data.status ===
                    'Quoted' && (
                    <button
                      className="primary-button"
                      disabled={
                        workflowMutation.isPending
                      }
                      onClick={() =>
                        workflowMutation.mutate(
                          {
                            action:
                              'approve',
                            jobId:
                              selectedJobQuery
                                .data!
                                .id,
                          },
                        )
                      }
                      type="button"
                    >
                      {workflowMutation.isPending
                        ? 'Updating...'
                        : 'Approve job'}
                    </button>
                  )}

                  {selectedJobQuery
                    .data.status ===
                    'Scheduled' && (
                    <button
                      className="primary-button"
                      disabled={
                        workflowMutation.isPending
                      }
                      onClick={() =>
                        workflowMutation.mutate(
                          {
                            action:
                              'start',
                            jobId:
                              selectedJobQuery
                                .data!
                                .id,
                          },
                        )
                      }
                      type="button"
                    >
                      {workflowMutation.isPending
                        ? 'Updating...'
                        : 'Start job'}
                    </button>
                  )}

                  {selectedJobQuery
                    .data.status ===
                    'InProgress' && (
                    <button
                      className="primary-button"
                      disabled={
                        workflowMutation.isPending
                      }
                      onClick={() =>
                        workflowMutation.mutate(
                          {
                            action:
                              'complete',
                            jobId:
                              selectedJobQuery
                                .data!
                                .id,
                          },
                        )
                      }
                      type="button"
                    >
                      {workflowMutation.isPending
                        ? 'Updating...'
                        : 'Complete job'}
                    </button>
                  )}

                  {![
                    'Completed',
                    'Cancelled',
                    'Invoiced',
                  ].includes(
                    selectedJobQuery
                      .data.status,
                  ) && (
                    <button
                      className="secondary-button job-cancel-action"
                      disabled={
                        workflowMutation.isPending
                      }
                      onClick={() =>
                        workflowMutation.mutate(
                          {
                            action:
                              'cancel',
                            jobId:
                              selectedJobQuery
                                .data!
                                .id,
                          },
                        )
                      }
                      type="button"
                    >
                      Cancel job
                    </button>
                  )}

                </div>

              </>

            ) : null}

          </div>

        </aside>
      )}

    </main>
  )
}

export default JobsPage