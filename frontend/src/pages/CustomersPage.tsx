import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Building2,
  Mail,
  MapPin,
  Phone,
  Plus,
  RefreshCw,
  Search,
  Users,
  X,
} from 'lucide-react'
import { createCustomer, getCustomers } from '../api/customers'
import type { Customer } from '../api/types'
import './CustomersPage.css'

interface CustomersPageProps {
  tenantId: string
}

function formatDate(value: string) {
  try {
    return new Date(value).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    })
  } catch {
    return value
  }
}

function getInitials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('') || 'CU'
}

function CustomersPage({ tenantId }: CustomersPageProps) {
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)

  // Customer Form State
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [addressLine, setAddressLine] = useState('')
  const [city, setCity] = useState('')
  const [formError, setFormError] = useState('')

  const customersQuery = useQuery({
    queryKey: ['customers', tenantId, search],
    queryFn: () => getCustomers(tenantId, search.trim() || undefined),
  })

  const createMutation = useMutation({
    mutationFn: () =>
      createCustomer(tenantId, {
        name: name.trim(),
        phone: phone.trim(),
        email: email.trim() || null,
        addressLine: addressLine.trim() || null,
        city: city.trim() || null,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['customers', tenantId],
      })
      closeCreateModal()
    },
    onError: (error) => {
      setFormError(
        error instanceof Error
          ? error.message
          : 'Failed to create customer. Please check your permissions and details.',
      )
    },
  })

  const customers = useMemo(() => customersQuery.data ?? [], [customersQuery.data])

  const totalCustomers = customers.length
  const uniqueCities = useMemo(() => {
    const cities = new Set(
      customers.map((c) => c.city?.trim()).filter((c): c is string => Boolean(c)),
    )
    return cities.size
  }, [customers])

  const customersWithEmail = useMemo(() => {
    return customers.filter((c) => Boolean(c.email?.trim())).length
  }, [customers])

  function openCreateModal() {
    setName('')
    setPhone('')
    setEmail('')
    setAddressLine('')
    setCity('')
    setFormError('')
    setCreateOpen(true)
  }

  function closeCreateModal() {
    setCreateOpen(false)
    setFormError('')
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError('')

    if (!name.trim()) {
      setFormError('Customer name is required.')
      return
    }

    if (!phone.trim()) {
      setFormError('Phone number is required.')
      return
    }

    createMutation.mutate()
  }

  return (
    <main className="dashboard customers-workspace">
      {/* Hero Header */}
      <section className="customers-hero">
        <div>
          <p className="eyebrow">Directory</p>
          <h1>Customers</h1>
          <p className="customers-hero-copy">
            Manage your client roster, contact details, and service locations.
          </p>
        </div>

        <div className="customers-hero-actions">
          <button
            className="secondary-button"
            onClick={() => customersQuery.refetch()}
            type="button"
            title="Refresh customer list"
          >
            <RefreshCw size={16} />
            Refresh
          </button>

          <button
            className="primary-button"
            onClick={openCreateModal}
            type="button"
          >
            <Plus size={17} />
            Add customer
          </button>
        </div>
      </section>

      {/* Summary KPI Cards */}
      <section className="customers-summary-grid">
        <article className="customers-summary-card">
          <div className="customers-summary-icon">
            <Users size={20} />
          </div>
          <div className="customers-summary-info">
            <span className="customers-summary-label">Total Clients</span>
            <strong className="customers-summary-value">{totalCustomers}</strong>
          </div>
        </article>

        <article className="customers-summary-card">
          <div className="customers-summary-icon">
            <Building2 size={20} />
          </div>
          <div className="customers-summary-info">
            <span className="customers-summary-label">Cities Served</span>
            <strong className="customers-summary-value">{uniqueCities}</strong>
          </div>
        </article>

        <article className="customers-summary-card">
          <div className="customers-summary-icon">
            <Mail size={20} />
          </div>
          <div className="customers-summary-info">
            <span className="customers-summary-label">Email Verified</span>
            <strong className="customers-summary-value">
              {customersWithEmail} <small style={{ fontSize: '13px', color: 'var(--muted)', fontWeight: 500 }}>/ {totalCustomers}</small>
            </strong>
          </div>
        </article>
      </section>

      {/* Toolbar & Search */}
      <section className="customers-toolbar">
        <div className="customers-search">
          <Search size={16} />
          <input
            aria-label="Search customers"
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name, phone, email..."
            type="search"
            value={search}
          />
        </div>
      </section>

      {/* Main Customers List Panel */}
      <section className="customers-panel">
        <header className="customers-panel-header">
          <div>
            <p className="eyebrow">Roster</p>
            <h2>Customer Directory</h2>
          </div>
          <span>
            {customersQuery.isFetching
              ? 'Refreshing...'
              : `${customers.length} client${customers.length === 1 ? '' : 's'}`}
          </span>
        </header>

        {customersQuery.isLoading ? (
          <div className="customers-state">
            <Users size={32} />
            <strong>Loading customers...</strong>
            <p>Fetching customer records from DispatchArc.</p>
          </div>
        ) : customersQuery.isError ? (
          <div className="customers-state">
            <Users size={32} />
            <strong>Unable to load customers</strong>
            <p>
              {customersQuery.error instanceof Error
                ? customersQuery.error.message
                : 'Please verify the backend is running and try again.'}
            </p>
            <button
              className="secondary-button"
              onClick={() => customersQuery.refetch()}
              type="button"
            >
              Try again
            </button>
          </div>
        ) : customers.length === 0 ? (
          <div className="customers-state">
            <Users size={32} />
            <strong>No customers found</strong>
            <p>
              {search.trim()
                ? `No customers matched "${search.trim()}". Try clearing your search.`
                : 'Get started by adding your first customer to the directory.'}
            </p>
            {!search.trim() && (
              <button
                className="primary-button"
                onClick={openCreateModal}
                type="button"
              >
                <Plus size={16} />
                Add customer
              </button>
            )}
          </div>
        ) : (
          <div className="customers-list">
            {customers.map((customer: Customer) => (
              <article className="customer-row" key={customer.id}>
                <div className="customer-avatar">
                  {getInitials(customer.name)}
                </div>

                <div className="customer-main">
                  <div className="customer-name-row">
                    <span className="customer-name">{customer.name}</span>
                    {customer.city && (
                      <span className="customer-city-badge">
                        <Building2 size={12} />
                        {customer.city}
                      </span>
                    )}
                  </div>

                  <div className="customer-details">
                    <a
                      className="customer-detail-item"
                      href={`tel:${customer.phone}`}
                      title={`Call ${customer.phone}`}
                    >
                      <Phone size={13} />
                      <span>{customer.phone}</span>
                    </a>

                    {customer.email && (
                      <a
                        className="customer-detail-item"
                        href={`mailto:${customer.email}`}
                        title={`Email ${customer.email}`}
                      >
                        <Mail size={13} />
                        <span>{customer.email}</span>
                      </a>
                    )}

                    {customer.addressLine && (
                      <span className="customer-detail-item" title={customer.addressLine}>
                        <MapPin size={13} />
                        <span>{customer.addressLine}</span>
                      </span>
                    )}
                  </div>
                </div>

                <div className="customer-meta">
                  <span>Joined {formatDate(customer.createdAtUtc)}</span>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {/* Create Customer Modal */}
      {createOpen && (
        <div className="customer-modal-backdrop">
          <section
            aria-labelledby="create-customer-title"
            aria-modal="true"
            className="customer-modal"
            role="dialog"
          >
            <header>
              <div>
                <p className="eyebrow">New Client</p>
                <h2 id="create-customer-title">Add Customer</h2>
                <p>Add a new customer to your dispatch workspace.</p>
              </div>

              <button
                aria-label="Close modal"
                className="customer-modal-close"
                onClick={closeCreateModal}
                type="button"
              >
                <X size={18} />
              </button>
            </header>

            <form onSubmit={handleSubmit}>
              <div className="customer-form-field">
                <span>Name *</span>
                <input
                  autoFocus
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Acme Industrial Corp"
                  required
                  type="text"
                  value={name}
                />
              </div>

              <div className="customer-form-row">
                <div className="customer-form-field">
                  <span>Phone *</span>
                  <input
                    onChange={(e) => setPhone(e.target.value)}
                    placeholder="e.g. (555) 234-5678"
                    required
                    type="tel"
                    value={phone}
                  />
                </div>

                <div className="customer-form-field">
                  <span>Email</span>
                  <input
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="e.g. contact@acme.com"
                    type="email"
                    value={email}
                  />
                </div>
              </div>

              <div className="customer-form-row">
                <div className="customer-form-field">
                  <span>Address Line</span>
                  <input
                    onChange={(e) => setAddressLine(e.target.value)}
                    placeholder="e.g. 100 Industrial Parkway"
                    type="text"
                    value={addressLine}
                  />
                </div>

                <div className="customer-form-field">
                  <span>City</span>
                  <input
                    onChange={(e) => setCity(e.target.value)}
                    placeholder="e.g. Seattle"
                    type="text"
                    value={city}
                  />
                </div>
              </div>

              {formError && (
                <div className="customer-form-error">
                  {formError}
                </div>
              )}

              <div className="customer-modal-actions">
                <button
                  className="secondary-button"
                  onClick={closeCreateModal}
                  type="button"
                >
                  Cancel
                </button>
                <button
                  className="primary-button"
                  disabled={createMutation.isPending}
                  type="submit"
                >
                  <Plus size={16} />
                  {createMutation.isPending ? 'Creating...' : 'Create customer'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  )
}

export default CustomersPage
