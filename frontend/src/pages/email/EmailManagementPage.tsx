import React, { useState, useMemo, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Mail, Send, Inbox, Archive, Search, Filter, RefreshCw, 
  Paperclip, Reply, ReplyAll, Forward, Trash2, Eye, 
  Calendar, User, FileText, CheckCircle, XCircle, Clock,
  Plus, X, Loader2, Download, Image as ImageIcon, AlertCircle,
  FileCheck, ExternalLink, TrendingUp
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { 
  getEmailAccounts, 
  pollEmailAccount
} from '../../api/email';
import type { EmailMailbox, EmailMessage, EmailAttachment } from '../../types/email';
import EmailMailboxesPage from '../../features/email/EmailMailboxesPage';
import { getEmails, getEmail, downloadAttachment, formatFileSize } from '../../api/emails';
import { 
  sendEmail, 
  sendEmailWithTemplate,
  sendRescheduleRequest,
  getEmailTemplatesByEntityType,
  type EmailTemplate,
  type SendEmailDto,
  type SendEmailWithTemplateDto,
  type SendRescheduleRequestDto,
  type EmailSendingResult
} from '../../api/emailSending';
import { getEmailTemplates } from '../../api/emailTemplates';
import { getParseSessions } from '../../api/parser';
import { PageShell } from '../../components/layout';
import { 
  Button, 
  Card, 
  LoadingSpinner, 
  EmptyState, 
  useToast,
  StatusBadge,
  Modal,
  Input,
  Textarea
} from '../../components/ui';

// Types imported from types/email.ts
import { formatLocalDateTime } from '../../utils/dateUtils';

const EmailManagementPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [activeTab, setActiveTab] = useState<'inbox' | 'sent' | 'compose' | 'accounts'>('inbox');
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null);
  const [imagesLoaded, setImagesLoaded] = useState(false);
  const [isComposeOpen, setIsComposeOpen] = useState(false);
  const [isReplyOpen, setIsReplyOpen] = useState(false);
  const [selectedMailboxId, setSelectedMailboxId] = useState<string>('');
  const [composeForm, setComposeForm] = useState<{
    emailAccountId: string;
    templateId?: string;
    to: string;
    cc?: string;
    bcc?: string;
    subject: string;
    body: string;
    placeholders?: Record<string, string>;
  }>({
    emailAccountId: '',
    to: '',
    subject: '',
    body: '',
  });

  // Fetch email accounts
  const { data: emailAccounts = [], isLoading: accountsLoading } = useQuery({
    queryKey: ['emailAccounts'],
    queryFn: getEmailAccounts,
  });

  // Filter available mailboxes based on RBAC
  const availableMailboxes = useMemo(() => {
    const userRoles = user?.roles || [];
    const isSuperAdmin = userRoles.includes('SuperAdmin');
    const isAdmin = userRoles.includes('Admin');

    if (isSuperAdmin) {
      // SuperAdmin can see all mailboxes
      return emailAccounts.filter(acc => acc.isActive);
    } else if (isAdmin) {
      // Admin can only see admin@cephas.com.my
      return emailAccounts.filter(acc => 
        acc.isActive && 
        (acc.emailAddress?.toLowerCase() === 'admin@cephas.com.my' || 
         acc.username?.toLowerCase() === 'admin@cephas.com.my')
      );
    } else {
      // Other roles: default to admin mailbox
      return emailAccounts.filter(acc => 
        acc.isActive && 
        (acc.emailAddress?.toLowerCase() === 'admin@cephas.com.my' || 
         acc.username?.toLowerCase() === 'admin@cephas.com.my')
      );
    }
  }, [emailAccounts, user?.roles]);

  // Set default mailbox based on RBAC
  useEffect(() => {
    if (availableMailboxes.length > 0 && !selectedMailboxId) {
      const userRoles = user?.roles || [];
      const isSuperAdmin = userRoles.includes('SuperAdmin');
      
      if (isSuperAdmin) {
        // SuperAdmin: default to admin@cephas.com.my if available, otherwise first available
        const adminMailbox = availableMailboxes.find(acc => 
          acc.emailAddress?.toLowerCase() === 'admin@cephas.com.my' ||
          acc.username?.toLowerCase() === 'admin@cephas.com.my'
        );
        setSelectedMailboxId(adminMailbox?.id || availableMailboxes[0]?.id || '');
      } else {
        // Admin and others: default to admin@cephas.com.my
        const adminMailbox = availableMailboxes.find(acc => 
          acc.emailAddress?.toLowerCase() === 'admin@cephas.com.my' ||
          acc.username?.toLowerCase() === 'admin@cephas.com.my'
        );
        setSelectedMailboxId(adminMailbox?.id || availableMailboxes[0]?.id || '');
      }
    }
  }, [availableMailboxes, selectedMailboxId, user?.roles]);

  // Get selected mailbox
  const selectedMailbox = useMemo(() => {
    return availableMailboxes.find(acc => acc.id === selectedMailboxId);
  }, [availableMailboxes, selectedMailboxId]);

  // Fetch email templates (only Outgoing templates for compose/reply)
  const { data: emailTemplates = [] } = useQuery({
    queryKey: ['emailTemplates', 'Outgoing'],
    queryFn: () => getEmailTemplates('Outgoing'),
  });

  // Fetch emails (inbox + sent) - list view (metadata only)
  const { data: emails = [], isLoading: emailsLoading, refetch: refetchEmails } = useQuery({
    queryKey: ['emails', activeTab, selectedMailboxId],
    queryFn: () => getEmails({
      direction: activeTab === 'inbox' ? 'Inbound' : 'Outbound',
      emailAccountId: selectedMailboxId || undefined
    }),
    enabled: !!selectedMailboxId, // Only fetch when a mailbox is selected
  });

  // Fetch full email details when selected
  const { data: selectedEmail, isLoading: emailDetailLoading, error: emailDetailError } = useQuery({
    queryKey: ['email', selectedEmailId],
    queryFn: () => getEmail(selectedEmailId!),
    enabled: !!selectedEmailId,
    staleTime: 30000, // Cache for 30 seconds
  });

  // Fetch parser statistics
  const { data: parseSessions = [] } = useQuery({
    queryKey: ['parseSessions'],
    queryFn: () => getParseSessions(),
    staleTime: 60000, // Cache for 1 minute
  });

  // Calculate parser statistics
  const parserStats = useMemo(() => {
    const completed = parseSessions.filter(s => s.status === 'Completed').length;
    const pending = parseSessions.filter(s => s.status === 'Pending').length;
    const errors = parseSessions.filter(s => s.status === 'Error').length;
    const totalDrafts = parseSessions.reduce((sum, s) => sum + (s.parsedOrdersCount || 0), 0);
    
    return {
      totalSessions: parseSessions.length,
      completed,
      pending,
      errors,
      totalDrafts
    };
  }, [parseSessions]);

  const sendMutation = useMutation({
    mutationFn: async (data: { useTemplate: boolean; formData: any; files?: File[] }) => {
      if (data.useTemplate && composeForm.templateId) {
        return await sendEmailWithTemplate(
          {
            templateId: composeForm.templateId,
            to: composeForm.to,
            cc: composeForm.cc,
            bcc: composeForm.bcc,
            placeholders: composeForm.placeholders,
          },
          data.files
        );
      } else {
        return await sendEmail(
          {
            emailAccountId: composeForm.emailAccountId,
            to: composeForm.to,
            subject: composeForm.subject,
            body: composeForm.body,
            cc: composeForm.cc,
            bcc: composeForm.bcc,
          },
          data.files
        );
      }
    },
    onSuccess: () => {
      showSuccess('Email sent successfully');
      setIsComposeOpen(false);
      setComposeForm({
        emailAccountId: '',
        to: '',
        subject: '',
        body: '',
      });
      queryClient.invalidateQueries({ queryKey: ['emails'] });
    },
    onError: (error: any) => {
      showError(`Failed to send email: ${error.message || 'An error occurred'}`);
    },
  });

  const pollMutation = useMutation({
    mutationFn: pollEmailAccount,
    onSuccess: (result) => {
      const fetchedCount = result?.emailsFetched || 0;
      const sessionsCount = result?.parseSessionsCreated || 0;
      if (fetchedCount > 0) {
        showSuccess(`Fetched ${fetchedCount} email(s)${sessionsCount > 0 ? `, created ${sessionsCount} parse session(s)` : ''}`);
      } else {
        showSuccess('No new emails found');
      }
      // Auto-refresh email list after polling
      queryClient.invalidateQueries({ queryKey: ['emails'] });
      // Also refresh parse sessions if any were created
      if (sessionsCount > 0) {
        queryClient.invalidateQueries({ queryKey: ['parseSessions'] });
      }
    },
    onError: (error: any) => {
      showError(`Failed to poll emails: ${error.message}`);
    },
  });

  const handleTemplateSelect = async (templateId: string) => {
    const template = emailTemplates.find(t => t.id === templateId);
    if (template) {
      setComposeForm({
        ...composeForm,
        templateId: template.id,
        subject: template.subjectTemplate,
        body: template.bodyTemplate,
      });
    }
  };

  const handleEmailSelect = (emailId: string) => {
    setSelectedEmailId(emailId);
    setImagesLoaded(false); // Reset image loading state when selecting new email
  };

  const handleDownloadAttachment = async (attachment: EmailAttachment) => {
    if (!selectedEmail) return;
    
    try {
      await downloadAttachment(selectedEmail.id, attachment.id, attachment.fileName);
      showSuccess(`Downloaded ${attachment.fileName}`);
    } catch (error: any) {
      showError(error.message || 'Failed to download attachment');
    }
  };

  const filteredEmails = emails.filter(email => {
    if (activeTab === 'inbox') return email.direction === 'Inbound';
    if (activeTab === 'sent') return email.direction === 'Outbound';
    return true;
  });

  const activeAccounts = emailAccounts.filter(acc => acc.isActive && acc.smtpHost);

  return (
    <PageShell
      title="Email Management"
      breadcrumbs={[{ label: 'Email' }]}
      actions={
        <div className="flex gap-2">
          <Button
            onClick={() => {
              if (selectedMailboxId) {
                pollMutation.mutate(selectedMailboxId);
              } else {
                showError('Please select a mailbox first');
              }
            }}
            disabled={!selectedMailboxId || pollMutation.isPending}
          >
            {pollMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Polling...
              </>
            ) : (
              <>
                <Mail className="h-4 w-4 mr-2" />
                Parse Now
              </>
            )}
          </Button>
          <Button
            onClick={() => navigate('/orders/parser')}
            variant="outline"
          >
            <FileCheck className="w-4 h-4 mr-2" />
            Parser Review
            {parserStats.totalDrafts > 0 && (
              <span className="ml-2 px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-200 text-xs font-semibold rounded-full">
                {parserStats.totalDrafts}
              </span>
            )}
          </Button>
          <Button
            onClick={() => setIsComposeOpen(true)}
            variant="default"
          >
            <Plus className="w-4 h-4 mr-2" />
            Compose
          </Button>
          <Button
            onClick={() => {
              if (selectedMailboxId) {
                pollMutation.mutate(selectedMailboxId);
              }
            }}
            variant="outline"
            disabled={pollMutation.isPending || !selectedMailboxId}
          >
            <RefreshCw className={`w-4 h-4 mr-2 ${pollMutation.isPending ? 'animate-spin' : ''}`} />
            Poll Inbox
          </Button>
        </div>
      }
    >
      {/* Parser Statistics Widget */}
      {activeTab === 'inbox' && parserStats.totalSessions > 0 && (
        <Card className="mb-6 p-4 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 border-blue-200 dark:border-blue-800">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="p-3 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                <FileCheck className="w-6 h-6 text-blue-600 dark:text-blue-400" />
              </div>
              <div>
                <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100 mb-1">
                  Email Parser Statistics
                </h3>
                <div className="flex items-center gap-4 text-xs text-slate-600 dark:text-slate-400">
                  <span className="flex items-center gap-1">
                    <TrendingUp className="w-3 h-3" />
                    {parserStats.totalSessions} sessions
                  </span>
                  <span className="flex items-center gap-1">
                    <CheckCircle className="w-3 h-3 text-green-600" />
                    {parserStats.completed} completed
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock className="w-3 h-3 text-yellow-600" />
                    {parserStats.pending} pending
                  </span>
                  <span className="flex items-center gap-1">
                    <FileText className="w-3 h-3 text-blue-600" />
                    {parserStats.totalDrafts} drafts
                  </span>
                </div>
              </div>
            </div>
            <Button
              onClick={() => navigate('/orders/parser')}
              variant="outline"
              size="sm"
            >
              View All
              <ExternalLink className="w-3 h-3 ml-1" />
            </Button>
          </div>
        </Card>
      )}

      {/* Mailbox Selection */}
      {activeTab !== 'accounts' && (
        <div className="mb-4 flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Mail className="w-4 h-4 text-slate-500" />
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
              Mailbox:
            </label>
            <select
              value={selectedMailboxId}
              onChange={(e) => setSelectedMailboxId(e.target.value)}
              className="px-3 py-1.5 border border-slate-300 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 text-sm min-w-[250px]"
            >
              <option value="">Select mailbox...</option>
              {availableMailboxes.map(acc => (
                <option key={acc.id} value={acc.id}>
                  {acc.name} ({acc.emailAddress || acc.username})
                </option>
              ))}
            </select>
            {selectedMailbox && (
              <span className="text-xs text-slate-500 dark:text-slate-400">
                {selectedMailbox.isActive ? (
                  <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                    <CheckCircle className="w-3 h-3" />
                    Active
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-slate-400">
                    <XCircle className="w-3 h-3" />
                    Inactive
                  </span>
                )}
              </span>
            )}
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-2 mb-6 border-b border-slate-200 dark:border-slate-700">
        <button
          onClick={() => setActiveTab('inbox')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors ${
            activeTab === 'inbox'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          }`}
        >
          <Inbox className="w-4 h-4 inline mr-2" />
          Inbox ({filteredEmails.filter(e => e.direction === 'Inbound').length})
        </button>
        <button
          onClick={() => setActiveTab('sent')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors ${
            activeTab === 'sent'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          }`}
        >
          <Send className="w-4 h-4 inline mr-2" />
          Sent ({filteredEmails.filter(e => e.direction === 'Outbound').length})
        </button>
        <button
          onClick={() => setActiveTab('accounts')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors ${
            activeTab === 'accounts'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          }`}
        >
          <Mail className="w-4 h-4 inline mr-2" />
          Email Accounts ({emailAccounts.length})
        </button>
      </div>

      {/* Accounts Tab Content */}
      {activeTab === 'accounts' && (
        <div>
          <EmailMailboxesPage />
        </div>
      )}

      {/* Email List and Detail (Inbox/Sent) */}
      {activeTab !== 'accounts' && (
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Email List */}
        <div className="lg:col-span-1">
          <Card className="p-0">
            {emailsLoading ? (
              <div className="p-8">
                <LoadingSpinner />
              </div>
            ) : !selectedMailboxId ? (
              <EmptyState
                icon={<Mail className="h-12 w-12" />}
                title="Select a mailbox"
                description="Please select a mailbox to view emails"
              />
            ) : filteredEmails.length === 0 ? (
              <EmptyState
                icon={<Mail className="h-12 w-12" />}
                title={`No ${activeTab} emails`}
                description={`No emails found in ${activeTab} for selected mailbox`}
              />
            ) : (
              <div className="divide-y divide-slate-200 dark:divide-slate-700 max-h-[600px] overflow-y-auto">
                {filteredEmails.map((email) => (
                  <div
                    key={email.id}
                    onClick={() => handleEmailSelect(email.id)}
                    className={`p-3 cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors ${
                      selectedEmailId === email.id ? 'bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-600' : ''
                    }`}
                  >
                    <div className="flex items-start gap-2 mb-1">
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-slate-900 dark:text-slate-100 truncate">
                          {activeTab === 'inbox' ? email.fromAddress : email.toAddresses}
                        </p>
                        <p className="text-xs text-slate-600 dark:text-slate-400 truncate mt-0.5">
                          {email.subject || '(no subject)'}
                        </p>
                        {email.bodyPreview && (
                          <p className="text-xs text-slate-500 dark:text-slate-500 truncate mt-1 line-clamp-1">
                            {email.bodyPreview}
                          </p>
                        )}
                      </div>
                      <div className="flex items-center gap-1 flex-shrink-0">
                        {email.hasAttachments && (
                          <Paperclip className="w-3.5 h-3.5 text-slate-400" />
                        )}
                        {email.isVip && (
                          <span className="text-yellow-500 text-xs">★</span>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center justify-between mt-1.5">
                      <span className="text-xs text-slate-500 dark:text-slate-400">
                        {formatLocalDateTime(email.receivedAt || email.sentAt)}
                      </span>
                      <div className="flex items-center gap-1">
                        {email.parserStatus === 'Parsed' && (
                          <StatusBadge variant="success" size="sm">Parsed</StatusBadge>
                        )}
                        {email.parserStatus === 'Processed' && (
                          <StatusBadge variant="success" size="sm">Processed</StatusBadge>
                        )}
                        {email.parserStatus === 'Error' && (
                          <StatusBadge variant="error" size="sm">Error</StatusBadge>
                        )}
                        {email.parserStatus === 'Pending' && email.hasAttachments && (
                          <StatusBadge variant="warning" size="sm">Pending</StatusBadge>
                        )}
                        {email.parserStatus === 'Sent' && (
                          <StatusBadge variant="info" size="sm">Sent</StatusBadge>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>

        {/* Email Detail / Reading Pane */}
        <div className="lg:col-span-2">
          {emailDetailLoading ? (
            <Card className="p-12">
              <div className="flex items-center justify-center">
                <LoadingSpinner />
              </div>
            </Card>
          ) : emailDetailError ? (
            <Card className="p-12">
              <EmptyState
                icon={<AlertCircle className="h-12 w-12 text-red-500" />}
                title="Email not available"
                description={emailDetailError instanceof Error ? emailDetailError.message : "This email has expired and is no longer available"}
              />
            </Card>
          ) : selectedEmail ? (
            <Card className="p-6 flex flex-col h-full">
              {/* Email Header */}
              <div className="mb-4 pb-4 border-b border-slate-200 dark:border-slate-700">
                <div className="flex items-start justify-between mb-3">
                  <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100 pr-4">
                    {selectedEmail.subject || '(no subject)'}
                  </h2>
                  <div className="flex gap-2 flex-shrink-0">
                    {activeTab === 'inbox' && (
                      <>
                        <Button 
                          variant="outline" 
                          size="sm"
                          onClick={() => {
                            setIsReplyOpen(true);
                            setComposeForm({
                              emailAccountId: activeAccounts[0]?.id || '',
                              to: selectedEmail.fromAddress,
                              subject: `Re: ${selectedEmail.subject}`,
                              body: `\n\n--- Original Message ---\nFrom: ${selectedEmail.fromAddress}\nDate: ${formatLocalDateTime(selectedEmail.receivedAt || selectedEmail.sentAt)}\n\n${selectedEmail.bodyPreview || ''}`,
                            });
                          }}
                        >
                          <Reply className="w-4 h-4 mr-2" />
                          Reply
                        </Button>
                        <Button variant="outline" size="sm">
                          <Forward className="w-4 h-4 mr-2" />
                          Forward
                        </Button>
                      </>
                    )}
                  </div>
                </div>
                <div className="space-y-1.5 text-sm">
                  <div className="flex">
                    <span className="font-medium text-slate-700 dark:text-slate-300 w-16 flex-shrink-0">From:</span>
                    <span className="text-slate-600 dark:text-slate-400">{selectedEmail.fromAddress}</span>
                  </div>
                  <div className="flex">
                    <span className="font-medium text-slate-700 dark:text-slate-300 w-16 flex-shrink-0">To:</span>
                    <span className="text-slate-600 dark:text-slate-400">{selectedEmail.toAddresses}</span>
                  </div>
                  {selectedEmail.ccAddresses && (
                    <div className="flex">
                      <span className="font-medium text-slate-700 dark:text-slate-300 w-16 flex-shrink-0">CC:</span>
                      <span className="text-slate-600 dark:text-slate-400">{selectedEmail.ccAddresses}</span>
                    </div>
                  )}
                  <div className="flex">
                    <span className="font-medium text-slate-700 dark:text-slate-300 w-16 flex-shrink-0">Date:</span>
                    <span className="text-slate-600 dark:text-slate-400">
                      {formatLocalDateTime(selectedEmail.receivedAt || selectedEmail.sentAt)}
                    </span>
                  </div>
                </div>
              </div>

              {/* Expired State */}
              {selectedEmail.isExpired && (
                <div className="mb-4 p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                  <div className="flex items-center gap-2">
                    <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
                    <p className="text-sm text-yellow-800 dark:text-yellow-200">
                      This message has expired and is no longer available.
                    </p>
                  </div>
                </div>
              )}

              {/* Attachments */}
              {selectedEmail.attachments && selectedEmail.attachments.length > 0 && (
                <div className="mb-4 pb-4 border-b border-slate-200 dark:border-slate-700">
                  <div className="flex items-center gap-2 mb-3">
                    <Paperclip className="w-4 h-4 text-slate-500" />
                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                      Attachments ({selectedEmail.attachments.length})
                    </span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    {selectedEmail.attachments
                      .filter(att => !att.isExpired)
                      .map((attachment) => (
                        <div
                          key={attachment.id}
                          className="flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700"
                        >
                          <div className="flex items-center gap-2 flex-1 min-w-0">
                            <FileText className="w-4 h-4 text-slate-400 flex-shrink-0" />
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium text-slate-900 dark:text-slate-100 truncate">
                                {attachment.fileName}
                              </p>
                              <p className="text-xs text-slate-500 dark:text-slate-400">
                                {formatFileSize(attachment.sizeBytes)}
                              </p>
                            </div>
                          </div>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleDownloadAttachment(attachment)}
                            className="ml-2 flex-shrink-0"
                          >
                            <Download className="w-3.5 h-3.5 mr-1" />
                            Download
                          </Button>
                        </div>
                      ))}
                  </div>
                </div>
              )}

              {/* Image Blocking Banner */}
              {selectedEmail.bodyHtml && !imagesLoaded && (
                <div className="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <ImageIcon className="w-4 h-4 text-blue-600 dark:text-blue-400" />
                      <p className="text-sm text-blue-800 dark:text-blue-200">
                        Images are blocked for privacy. Click to load images.
                      </p>
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setImagesLoaded(true)}
                    >
                      Load Images
                    </Button>
                  </div>
                </div>
              )}

              {/* Email Body */}
              <div className="flex-1 overflow-y-auto">
                <div 
                  className="prose dark:prose-invert max-w-none text-sm"
                  style={{
                    // Block images if not loaded
                    ...(!imagesLoaded && {
                      pointerEvents: 'none',
                    })
                  }}
                  dangerouslySetInnerHTML={{ 
                    __html: (() => {
                      let html = selectedEmail.bodyHtml || selectedEmail.bodyText || selectedEmail.bodyPreview || 'No content';
                      
                      // If images are blocked, replace img src with placeholder
                      if (!imagesLoaded && selectedEmail.bodyHtml) {
                        html = html.replace(
                          /<img([^>]*?)src=["']([^"']+)["']([^>]*?)>/gi,
                          (match, before, src, after) => {
                            // Keep inline/CID images (they're attachments)
                            if (src.startsWith('cid:') || selectedEmail.attachments?.some(a => a.contentId && src.includes(a.contentId))) {
                              return match;
                            }
                            // Block external images
                            return `<img${before}src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1' height='1'%3E%3C/svg%3E" data-original-src="${src}"${after}>`;
                          }
                        );
                      }
                      
                      return html;
                    })()
                  }}
                />
              </div>
            </Card>
          ) : (
            <Card className="p-12">
              <EmptyState
                icon={<Mail className="h-12 w-12" />}
                title="Select an email"
                description="Choose an email from the list to view its contents"
              />
            </Card>
          )}
        </div>
      </div>
      )}

      {/* Compose Modal */}
      <Modal
        isOpen={isComposeOpen}
        onClose={() => setIsComposeOpen(false)}
        title="Compose Email"
        size="large"
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              From (Email Account)
            </label>
            <select
              value={composeForm.emailAccountId}
              onChange={(e) => setComposeForm({ ...composeForm, emailAccountId: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
            >
              <option value="">Select account</option>
              {activeAccounts.map(acc => (
                <option key={acc.id} value={acc.id}>
                  {acc.name} ({acc.emailAddress})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Use Template (Optional)
            </label>
            <select
              value={composeForm.templateId || ''}
              onChange={(e) => {
                if (e.target.value) {
                  handleTemplateSelect(e.target.value);
                } else {
                  setComposeForm({ ...composeForm, templateId: undefined });
                }
              }}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
            >
              <option value="">No template</option>
              {emailTemplates.filter(t => t.isActive).map(template => (
                <option key={template.id} value={template.id}>
                  {template.name} ({template.code})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              To
            </label>
            <Input
              value={composeForm.to}
              onChange={(e) => setComposeForm({ ...composeForm, to: e.target.value })}
              placeholder="recipient@example.com"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              CC (Optional)
            </label>
            <Input
              value={composeForm.cc || ''}
              onChange={(e) => setComposeForm({ ...composeForm, cc: e.target.value })}
              placeholder="cc@example.com"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Subject
            </label>
            <Input
              value={composeForm.subject}
              onChange={(e) => setComposeForm({ ...composeForm, subject: e.target.value })}
              placeholder="Email subject"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Body (HTML supported)
            </label>
            <Textarea
              value={composeForm.body}
              onChange={(e) => setComposeForm({ ...composeForm, body: e.target.value })}
              rows={12}
              placeholder="Email body (HTML supported)"
              className="font-mono text-sm"
            />
            {composeForm.templateId && (
              <p className="text-xs text-slate-500 mt-1">
                Template placeholders: {Object.keys(composeForm.placeholders || {}).join(', ') || 'None'}
              </p>
            )}
          </div>

          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => setIsComposeOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={() => sendMutation.mutate({
                useTemplate: !!composeForm.templateId,
                formData: composeForm
              })}
              disabled={!composeForm.emailAccountId || !composeForm.to || sendMutation.isPending}
            >
              {sendMutation.isPending ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Sending...
                </>
              ) : (
                <>
                  <Send className="w-4 h-4 mr-2" />
                  Send
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>
    </PageShell>
  );
};

export default EmailManagementPage;

