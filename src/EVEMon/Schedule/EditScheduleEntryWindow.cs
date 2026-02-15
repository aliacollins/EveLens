using System;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Scheduling;

namespace EVEMon.Schedule
{
    public partial class EditScheduleEntryWindow : EVEMonForm
    {
        private ScheduleEntry m_scheduleEntry = null!;

        private DateTime m_recurringDateFrom;
        private DateTime m_recurringDateTo;
        private DateTime m_oneTimeStartDate;
        private DateTime m_oneTimeEndDate;
        private int m_oneTimeStartTime;
        private int m_oneTimeEndTime;
        private int m_recurringStartTime;
        private int m_recurringEndTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditScheduleEntryWindow"/> class.
        /// </summary>
        public EditScheduleEntryWindow()
        {
            InitializeComponent();

            tbTitle.MaxLength = ScheduleEntry.TitleMaxLength;

            if (!EveMonClient.IsDebugBuild)
                buttonDebug.Visible = false;

            InitialEntry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditScheduleEntryWindow"/> class.
        /// </summary>
        /// <param name="defaultTime">The default time.</param>
        public EditScheduleEntryWindow(DateTime defaultTime)
            : this()
        {
            DateTime start = defaultTime;
            DateTime end = defaultTime.AddDays(1).AddMinutes(-1);

            SetOneTimeStartDate(start);
            SetOneTimeEndDate(end);

            SetRecurringDateFrom(start);
            SetRecurringDateTo(end);
        }

        /// <summary>
        /// Gets or sets the schedule entry.
        /// </summary>
        /// <value>The schedule entry.</value>
        public ScheduleEntry ScheduleEntry
        {
            get { return m_scheduleEntry; }
            set
            {
                m_scheduleEntry = value;
                UpdateFromEntry();
            }
        }

        /// <summary>
        /// Initials the entry.
        /// </summary>
        private void InitialEntry()
        {
            tbTitle.Text = String.Empty;
            SetTypeFlags(ScheduleEntryOptions.None);
            rbOneTime.Checked = true;
            rbRecurring.Checked = false;
            SetOneTimeStartDate(DateTime.Today);
            tbOneTimeStartTime.Text = DateTime.Today.ToShortTimeString();
            DateTime dtto = DateTime.Today.AddDays(1).AddMinutes(-1);
            SetOneTimeEndDate(dtto);
            tbOneTimeEndTime.Text = dtto.ToShortTimeString();
            SetRecurringDateFrom(DateTime.MinValue);
            SetRecurringDateTo(DateTime.MaxValue);
            cbRecurringFrequency.SelectedIndex = 0;
            nudWeeklyFrequency.Value = 1;
            nudRecurDayOfMonth.Value = 1;
            cbRecurOnOverflow.SelectedIndex = 0;
            tbRecurringTimeFrom.Text = DateTime.Today.ToShortTimeString();
            tbRecurringTimeTo.Text = dtto.ToShortTimeString();
        }

        /// <summary>
        /// Updates from entry.
        /// </summary>
        private void UpdateFromEntry()
        {
            if (m_scheduleEntry == null)
                return;

            tbTitle.Text = m_scheduleEntry.Title;
            SetTypeFlags(m_scheduleEntry.Options);

            SimpleScheduleEntry? sse = m_scheduleEntry as SimpleScheduleEntry;
            RecurringScheduleEntry? rse = m_scheduleEntry as RecurringScheduleEntry;

            if (sse != null)
            {
                rbOneTime.Checked = true;
                rbRecurring.Checked = false;
                SetOneTimeStartDate(sse.StartDate);
                tbOneTimeStartTime.Text = sse.StartDate.ToShortTimeString();
                SetOneTimeEndDate(sse.EndDate);
                tbOneTimeEndTime.Text = sse.EndDate.ToShortTimeString();
            }
            else if (rse != null)
            {
                rbOneTime.Checked = false;
                rbRecurring.Checked = true;
                SetRecurringDateFrom(rse.StartDate);
                SetRecurringDateTo(rse.EndDate);
                SetRecurringFrequencyDropdown(rse.Frequency, rse.DayOfWeek, rse.WeeksPeriod);
                nudRecurDayOfMonth.Value = rse.DayOfMonth;
                SetRecurringOverflowDropdown(rse.OverflowResolution);
                DateTime tstart = DateTime.Today.AddSeconds(rse.StartTimeInSeconds);
                DateTime tend = DateTime.Today.AddSeconds(rse.EndTimeInSeconds);
                tbRecurringTimeFrom.Text = tstart.ToShortTimeString();
                tbRecurringTimeTo.Text = tend.ToShortTimeString();
            }

            ValidateData();
        }

        /// <summary>
        /// Sets the recurring overflow dropdown.
        /// </summary>
        /// <param name="monthlyOverflowResolution">The monthly overflow resolution.</param>
        private void SetRecurringOverflowDropdown(MonthlyOverflowResolution monthlyOverflowResolution)
        {
            switch (monthlyOverflowResolution)
            {
                case MonthlyOverflowResolution.Drop:
                    cbRecurOnOverflow.SelectedIndex = 0;
                    break;
                case MonthlyOverflowResolution.OverlapForward:
                    cbRecurOnOverflow.SelectedIndex = 1;
                    break;
                case MonthlyOverflowResolution.ClipBack:
                    cbRecurOnOverflow.SelectedIndex = 2;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the recurring overflow dropdown.
        /// </summary>
        /// <returns></returns>
        private MonthlyOverflowResolution GetRecurringOverflowDropdown()
        {
            switch (cbRecurOnOverflow.SelectedIndex)
            {
                case 0:
                    return MonthlyOverflowResolution.Drop;
                case 1:
                    return MonthlyOverflowResolution.OverlapForward;
                case 2:
                    return MonthlyOverflowResolution.ClipBack;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sets the recurring frequency dropdown.
        /// </summary>
        /// <param name="recurFrequency">The recur frequency.</param>
        /// <param name="recurDow">The recur dow.</param>
        /// <param name="nWeekly">The n weekly.</param>
        private void SetRecurringFrequencyDropdown(RecurringFrequency recurFrequency, DayOfWeek recurDow, int nWeekly)
        {
            switch (recurFrequency)
            {
                case RecurringFrequency.Daily:
                    cbRecurringFrequency.SelectedIndex = 0;
                    break;
                case RecurringFrequency.Weekdays:
                    cbRecurringFrequency.SelectedIndex = 1;
                    break;
                case RecurringFrequency.Weekends:
                    cbRecurringFrequency.SelectedIndex = 2;
                    break;
                case RecurringFrequency.Weekly:
                    switch (recurDow)
                    {
                        case DayOfWeek.Monday:
                            cbRecurringFrequency.SelectedIndex = 3;
                            break;
                        case DayOfWeek.Tuesday:
                            cbRecurringFrequency.SelectedIndex = 4;
                            break;
                        case DayOfWeek.Wednesday:
                            cbRecurringFrequency.SelectedIndex = 5;
                            break;
                        case DayOfWeek.Thursday:
                            cbRecurringFrequency.SelectedIndex = 6;
                            break;
                        case DayOfWeek.Friday:
                            cbRecurringFrequency.SelectedIndex = 7;
                            break;
                        case DayOfWeek.Saturday:
                            cbRecurringFrequency.SelectedIndex = 8;
                            break;
                        case DayOfWeek.Sunday:
                            cbRecurringFrequency.SelectedIndex = 9;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    nudWeeklyFrequency.Value = nWeekly;
                    break;
                case RecurringFrequency.Monthly:
                    cbRecurringFrequency.SelectedIndex = 10;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the recurring frequency dropdown.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private RecurringFrequency GetRecurringFrequencyDropdown()
        {
            switch (cbRecurringFrequency.SelectedIndex)
            {
                case 0:
                    return RecurringFrequency.Daily;
                case 1:
                    return RecurringFrequency.Weekdays;
                case 2:
                    return RecurringFrequency.Weekends;
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    return RecurringFrequency.Weekly;
                case 10:
                    return RecurringFrequency.Monthly;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the recurring frequency dropdown.
        /// </summary>
        /// <returns></returns>
        private DayOfWeek GetRecurringFrequencyDayOfWeek()
        {
            switch (cbRecurringFrequency.SelectedIndex)
            {
                case 4:
                    return DayOfWeek.Tuesday;
                case 5:
                    return DayOfWeek.Wednesday;
                case 6:
                    return DayOfWeek.Thursday;
                case 7:
                    return DayOfWeek.Friday;
                case 8:
                    return DayOfWeek.Saturday;
                case 9:
                    return DayOfWeek.Sunday;
                default:
                    return DayOfWeek.Monday;
            }
        }

        /// <summary>
        /// Sets the type flags.
        /// </summary>
        /// <param name="scheduleEntryOptions">The schedule entry options.</param>
        private void SetTypeFlags(ScheduleEntryOptions scheduleEntryOptions)
        {
            cbBlocking.Checked = (scheduleEntryOptions & ScheduleEntryOptions.Blocking) != 0;
            cbSilent.Checked = (scheduleEntryOptions & ScheduleEntryOptions.Quiet) != 0;
            cbUseEVETime.Checked = (scheduleEntryOptions & ScheduleEntryOptions.EVETime) != 0;
        }

        /// <summary>
        /// Gets the type flags.
        /// </summary>
        /// <returns></returns>
        private ScheduleEntryOptions GetTypeFlags()
        {
            ScheduleEntryOptions result = ScheduleEntryOptions.None;
            if (cbBlocking.Checked)
                result |= ScheduleEntryOptions.Blocking;

            if (cbSilent.Checked)
                result |= ScheduleEntryOptions.Quiet;

            if (cbUseEVETime.Checked)
                result |= ScheduleEntryOptions.EVETime;

            return result;
        }

        /// <summary>
        /// Sets the recurring date to.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        private void SetRecurringDateTo(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            {
                tbRecurringEndDate.Text = "(Forever)";
                m_recurringDateTo = DateTime.MaxValue;
                return;
            }

            tbRecurringEndDate.Text = dateTime.ToLongDateString();
            m_recurringDateTo = dateTime.Date;
        }

        /// <summary>
        /// Sets the recurring date from.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        private void SetRecurringDateFrom(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            {
                tbRecurringStartDate.Text = "(Forever)";
                m_recurringDateFrom = DateTime.MinValue;
                return;
            }

            tbRecurringStartDate.Text = dateTime.ToLongDateString();
            m_recurringDateFrom = dateTime.Date;
        }

        /// <summary>
        /// Sets the one time start date.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        private void SetOneTimeStartDate(DateTime dateTime)
        {
            tbOneTimeStartDate.Text = dateTime.ToLongDateString();
            m_oneTimeStartDate = dateTime.Date;
        }

        /// <summary>
        /// Sets the one time end date.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        private void SetOneTimeEndDate(DateTime dateTime)
        {
            tbOneTimeEndDate.Text = dateTime.ToLongDateString();
            m_oneTimeEndDate = dateTime.Date;
        }

        /// <summary>
        /// Handles the CheckedChanged event of the rbOneTime control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void rbOneTime_CheckedChanged(object? sender, EventArgs e)
        {
            pnlOneTime.Enabled = rbOneTime.Checked;

            ValidateData();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the rbRecurring control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void rbRecurring_CheckedChanged(object? sender, EventArgs e)
        {
            pnlRecurring.Enabled = rbRecurring.Checked;

            ValidateData();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cbRecurringFrequency control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void cbRecurringFrequency_SelectedIndexChanged(object? sender, EventArgs e)
        {
            pnlRecurMonthly.Enabled = cbRecurringFrequency.SelectedIndex == 10;
            pnlRecurWeekly.Enabled = cbRecurringFrequency.SelectedIndex >= 3 && cbRecurringFrequency.SelectedIndex <= 9;
        }

        /// <summary>
        /// Handles the Click event of the btnRecurringNoStartDate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRecurringNoStartDate_Click(object? sender, EventArgs e)
        {
            SetRecurringDateFrom(DateTime.MinValue);
        }

        /// <summary>
        /// Handles the Click event of the btnRecurringNoEndDate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRecurringNoEndDate_Click(object? sender, EventArgs e)
        {
            SetRecurringDateTo(DateTime.MaxValue);
        }

        /// <summary>
        /// Validates the data.
        /// </summary>
        private void ValidateData()
        {
            bool valid = true;
            if (String.IsNullOrEmpty(tbTitle.Text) || String.IsNullOrEmpty(tbTitle.Text.Trim()))
                valid = false;
            else
            {
                if (rbOneTime.Checked)
                {
                    int startSec;
                    int endSec;
                    if (!Scheduler.TryParseTime(tbOneTimeStartTime.Text, out startSec) ||
                        !Scheduler.TryParseTime(tbOneTimeEndTime.Text, out endSec))
                        valid = false;
                    else
                    {
                        DateTime startDate = m_oneTimeStartDate.AddSeconds(startSec);
                        DateTime endDate = m_oneTimeEndDate.AddSeconds(endSec);
                        if (startDate >= endDate)
                            valid = false;
                        else
                        {
                            m_oneTimeStartTime = startSec;
                            m_oneTimeEndTime = endSec;
                        }
                    }
                }
                else if (rbRecurring.Checked)
                {
                    if (m_recurringDateFrom > m_recurringDateTo)
                        valid = false;
                    else
                    {
                        int startSec;
                        int endSec;
                        if (!Scheduler.TryParseTime(tbRecurringTimeFrom.Text, out startSec) ||
                            !Scheduler.TryParseTime(tbRecurringTimeTo.Text, out endSec))
                            valid = false;
                        else
                        {
                            if (startSec >= endSec)
                                endSec += RecurringScheduleEntry.SecondsPerDay;

                            m_recurringStartTime = startSec;
                            m_recurringEndTime = endSec;
                        }
                    }
                }
                else
                    valid = false;
            }

            btnOk.Enabled = valid;
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object? sender, EventArgs e)
        {
            m_scheduleEntry = GenerateScheduleEntry();
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Generates the schedule entry.
        /// </summary>
        /// <returns></returns>
        private ScheduleEntry GenerateScheduleEntry()
        {
            ScheduleEntry? result = null;
            if (rbOneTime.Checked)
            {
                SimpleScheduleEntry sse = new SimpleScheduleEntry
                                              {
                                                  StartDate = new DateTime(
                                                      m_oneTimeStartDate.AddSeconds(m_oneTimeStartTime).Ticks,
                                                      DateTimeKind.Unspecified),
                                                  EndDate = new DateTime(
                                                      m_oneTimeEndDate.AddSeconds(m_oneTimeEndTime).Ticks,
                                                      DateTimeKind.Unspecified)
                                              };

                result = sse;
            }
            else if (rbRecurring.Checked)
            {
                RecurringScheduleEntry rse = new RecurringScheduleEntry
                                                 {
                                                     StartDate = m_recurringDateFrom,
                                                     EndDate = m_recurringDateTo,
                                                     StartTimeInSeconds = m_recurringStartTime,
                                                     EndTimeInSeconds = m_recurringEndTime,
                                                     DayOfMonth = Convert.ToInt32(nudRecurDayOfMonth.Value),
                                                     OverflowResolution = GetRecurringOverflowDropdown(),
                                                     Frequency = GetRecurringFrequencyDropdown(),
                                                     DayOfWeek = GetRecurringFrequencyDayOfWeek()
                                                 };

                if (rse.Frequency == RecurringFrequency.Weekly)
                    rse.WeeksPeriod = Convert.ToInt32(nudWeeklyFrequency.Value);

                result = rse;
            }

            if (result == null)
                return null!;

            result.Title = tbTitle.Text;
            result.Options = GetTypeFlags();

            return result;
        }

        /// <summary>
        /// Handles the Click event of the button1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonDebug_Click(object? sender, EventArgs e)
        {
            if (!btnOk.Enabled)
                return;

            ScheduleEntry ise = GenerateScheduleEntry();
            ScheduleEntry = ise;
        }

        /// <summary>
        /// Handles the TextChanged event of the tbTitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tbTitle_TextChanged(object? sender, EventArgs e)
        {
            ValidateData();
        }

        /// <summary>
        /// Handles the TextChanged event of the tbOneTimeStartTime control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tbOneTimeStartTime_TextChanged(object? sender, EventArgs e)
        {
            ValidateData();
        }

        /// <summary>
        /// Handles the TextChanged event of the tbOneTimeEndTime control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tbOneTimeEndTime_TextChanged(object? sender, EventArgs e)
        {
            ValidateData();
        }

        /// <summary>
        /// Handles the TextChanged event of the tbRecurringTimeFrom control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tbRecurringTimeFrom_TextChanged(object? sender, EventArgs e)
        {
            ValidateData();
        }

        /// <summary>
        /// Handles the TextChanged event of the tbRecurringTimeTo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tbRecurringTimeTo_TextChanged(object? sender, EventArgs e)
        {
            ValidateData();
        }

        /// <summary>
        /// Handles the Click event of the btnOneTimeStartDateChoose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOneTimeStartDateChoose_Click(object? sender, EventArgs e)
        {
            m_oneTimeStartDate = GetDate(m_oneTimeStartDate);
            SetOneTimeStartDate(m_oneTimeStartDate);
            ValidateData();
        }

        /// <summary>
        /// Handles the Click event of the btnOneTimeEndDateChoose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOneTimeEndDateChoose_Click(object? sender, EventArgs e)
        {
            m_oneTimeEndDate = GetDate(m_oneTimeEndDate);
            SetOneTimeEndDate(m_oneTimeEndDate);
            ValidateData();
        }

        /// <summary>
        /// Handles the Click event of the btnRecurringStartDateChoose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRecurringStartDateChoose_Click(object? sender, EventArgs e)
        {
            m_recurringDateFrom = GetDate(m_recurringDateFrom);
            SetRecurringDateFrom(m_recurringDateFrom);
            ValidateData();
        }

        /// <summary>
        /// Handles the Click event of the btnRecurringEndDateChoose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRecurringEndDateChoose_Click(object? sender, EventArgs e)
        {
            m_recurringDateTo = GetDate(m_recurringDateTo);
            SetRecurringDateTo(m_recurringDateTo);
            ValidateData();
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="res">The res.</param>
        /// <returns></returns>
        private static DateTime GetDate(DateTime res)
        {
            using (DateSelectWindow f = new DateSelectWindow())
            {
                if (res == DateTime.MinValue || res == DateTime.MaxValue)
                    f.SelectedDate = DateTime.Today;
                else
                    f.SelectedDate = res;

                DialogResult dr = f.ShowDialog();

                return dr == DialogResult.Cancel ? res : f.SelectedDate;
            }
        }
    }
}