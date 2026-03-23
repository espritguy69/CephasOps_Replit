/**
 * InvoiceDocument - Accounting-style invoice template (DEPRECATED)
 * Bill To, metadata, line items (4-line description), footer.
 *
 * Print and PDF now use Document Templates (server-rendered HTML).
 * This component is kept for reference/fallback only.
 * @deprecated Use server preview-html endpoint and Document Templates instead
 */

import React from 'react';
import type { Invoice, InvoiceLineItem } from '../../../types/billing';
import './invoice-document.css';

export interface InvoiceDocumentProps {
  invoice: Invoice;
  currency?: string;
  locale?: string;
}

const formatCurrency = (amount: number, currency = 'MYR', locale = 'en-MY'): string =>
  new Intl.NumberFormat(locale, { style: 'currency', currency }).format(amount);

const formatDate = (dateStr: string | null | undefined): string => {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('en-MY', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

/** Build 4-line description block for line item */
const getLineItemDescription = (item: InvoiceLineItem): string => {
  const hasOrderData =
    item.customerName ?? item.serviceId ?? item.orderType ?? item.docketNo;
  if (hasOrderData) {
    return [
      `CUSTOMER NAME: ${item.customerName ?? ''}`,
      `SERVICE ID: ${item.serviceId ?? ''}`,
      `ORDER TYPE: ${item.orderType ?? ''}`,
      `DOCKET NO: ${item.docketNo ?? ''}`,
    ].join('\n');
  }
  return item.description || '—';
};

const InvoiceDocument: React.FC<InvoiceDocumentProps> = ({
  invoice,
  currency = 'MYR',
  locale = 'en-MY',
}) => {
  const letterhead = invoice.companyLetterhead;
  const lineItems = invoice.lineItems ?? [];
  const subTotal = invoice.subTotal ?? (invoice.totalAmount - (invoice.taxAmount ?? 0));
  const taxAmount = invoice.taxAmount ?? 0;
  const grandTotal = invoice.totalAmount ?? 0;

  return (
    <div className="invoice-document no-break">
      {/* Company letterhead */}
      {letterhead && (
        <div className="letterhead">
          <h1>{letterhead.name}</h1>
          {letterhead.address && (
            <div className="letterhead-detail">{letterhead.address}</div>
          )}
          {(letterhead.phone || letterhead.email) && (
            <div className="letterhead-detail">
              {[letterhead.phone, letterhead.email].filter(Boolean).join(' | ')}
            </div>
          )}
          {letterhead.registrationNo && (
            <div className="letterhead-detail">
              Registration: {letterhead.registrationNo}
            </div>
          )}
        </div>
      )}

      <h2 style={{ margin: '0 0 16px 0', fontSize: '16pt', fontWeight: 700 }}>
        INVOICE
      </h2>

      {/* Bill To (left) + Invoice metadata (right) */}
      <div className="invoice-meta">
        <div className="bill-to">
          <h3>Bill To</h3>
          <div className="client-name">{invoice.partnerName || 'N/A'}</div>
          {invoice.partnerAddress &&
            invoice.partnerAddress.split('\n').map((line, i) => (
              <div key={i} className="client-detail">
                {line.trim()}
              </div>
            ))}
          <div className="client-detail">
            Person in charge: {invoice.partnerContactName || 'Finance Department'}
          </div>
          {invoice.partnerContactPhone && (
            <div className="client-detail">TEL: {invoice.partnerContactPhone}</div>
          )}
          <div className="client-detail">
            Subject: {invoice.billToSubject || 'Non Prelaid Activation'}
          </div>
        </div>

        <div className="invoice-info">
          <div className="info-row">
            <span className="info-label">Date Issued:</span>
            <span>{formatDate(invoice.invoiceDate)}</span>
          </div>
          <div className="info-row">
            <span className="info-label">Invoice No.:</span>
            <span>{invoice.invoiceNumber}</span>
          </div>
          {invoice.doRefNo && (
            <div className="info-row">
              <span className="info-label">DO Ref. No.:</span>
              <span>{invoice.doRefNo}</span>
            </div>
          )}
          {invoice.purchaseOrderNo && (
            <div className="info-row">
              <span className="info-label">Purchase Order No.:</span>
              <span>{invoice.purchaseOrderNo}</span>
            </div>
          )}
          <div className="info-row">
            <span className="info-label">Terms:</span>
            <span>Net 45 days</span>
          </div>
          <div className="info-row">
            <span className="info-label">Prepared By:</span>
            <span>Cephas Admin</span>
          </div>
          <div className="info-row">
            <span className="info-label">Due Date:</span>
            <span>{formatDate(invoice.dueDate ?? null)}</span>
          </div>
        </div>
      </div>

      {/* Line items: No | Description | Qty/Unit | Unit Price | Discount | Total */}
      <table className="line-items-table">
        <thead>
          <tr>
            <th className="col-no">No</th>
            <th className="col-desc">Description</th>
            <th className="col-qty text-right">Qty/Unit</th>
            <th className="col-price text-right">Unit Price</th>
            <th className="col-discount text-right">Discount</th>
            <th className="col-total text-right">Total</th>
          </tr>
        </thead>
        <tbody>
          {lineItems.length > 0 ? (
            lineItems.map((item, index) => (
              <tr key={item.id || index}>
                <td className="col-no">{index + 1}</td>
                <td className="col-desc">
                  <pre className="desc-block">
                    {getLineItemDescription(item)}
                  </pre>
                </td>
                <td className="col-qty text-right">{item.quantity}</td>
                <td className="col-price text-right">
                  {formatCurrency(item.unitPrice, currency, locale)}
                </td>
                <td className="col-discount text-right">
                  {formatCurrency(0, currency, locale)}
                </td>
                <td className="col-total text-right">
                  {formatCurrency(
                    item.total ?? item.quantity * item.unitPrice,
                    currency,
                    locale
                  )}
                </td>
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={6} style={{ textAlign: 'center', padding: '24px', color: '#9ca3af' }}>
                No line items
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Totals */}
      <div className="totals">
        <div className="total-row">
          <span className="label">Subtotal:</span>
          <span>{formatCurrency(subTotal, currency, locale)}</span>
        </div>
        <div className="total-row">
          <span className="label">Tax (GST):</span>
          <span>{formatCurrency(taxAmount, currency, locale)}</span>
        </div>
        <div className="total-row grand-total">
          <span>Total:</span>
          <span>{formatCurrency(grandTotal, currency, locale)}</span>
        </div>
      </div>

      {/* Footer - accounting style */}
      <div className="footer no-break">
        <div>Bank Name: AgroBank</div>
        <div>Bank Account Number: 1005511000058559</div>
        <div>Payable to: CEPHAS TRADING & SERVICES</div>
        <p className="footer-disclaimer">
          &quot;This is a computer generated document. No signature is required.&quot;
        </p>
      </div>
    </div>
  );
};

export default InvoiceDocument;
