using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using EVEMon.Common.Collections.Global;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;

namespace EVEMon.ApiCredentialsManagement
{
    /// <summary>
    /// A windows to import characters from files or urls.
    /// </summary>
    public partial class CharacterImportationWindow : EVEMonForm
    {
        // When multiple downloads are fired, we only want to react to the latest one, this is done through versioning.
        private int m_version;
        private UriCharacterEventArgs m_args = null!;
        private UriCharacter m_uriCharacter = null!;

        /// <summary>
        /// Constructor for a new character creation.
        /// </summary>
        public CharacterImportationWindow()
        {
            InitializeComponent();
            errorPanel.Visible = false;
            namePanel.Visible = false;
            okButton.Enabled = false;
        }

        /// <summary>
        /// Constructor to update a character.
        /// </summary>
        public CharacterImportationWindow(UriCharacter uriCharacter)
            : this()
        {
            m_uriCharacter = uriCharacter;
        }

        /// <summary>
        /// On load, subscribe events.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            urlTextBox.TextChanged += urlTextBox_TextChanged;

            if (m_uriCharacter == null)
                return;

            if (m_uriCharacter.Uri != null && m_uriCharacter.Uri.IsFile)
            {
                fileTextBox.Text = m_uriCharacter.Uri.ToString();
                fileRadio.Checked = true;
            }
            else if (m_uriCharacter.Uri != null)
            {
                urlTextBox.Text = m_uriCharacter.Uri.ToString();
                urlRadio.Checked = true;
            }
        }

        /// <summary>
        /// Enable or disable the proper controls when the user changes the radio buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radio_CheckedChanged(object? sender, EventArgs e)
        {
            urlTextBox.Enabled = urlRadio.Checked;
            urlThrobber.Enabled = urlRadio.Checked;

            fileButton.Enabled = fileRadio.Checked;
            fileTextBox.Enabled = fileRadio.Checked;
        }

        /// <summary>
        /// When the users changes the url, we query from the web.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void urlTextBox_TextChanged(object? sender, EventArgs e)
        {
            delayQueryTimer.Stop();
            delayQueryTimer.Start();
        }

        /// <summary>
        /// We would like to check the provided url anytime the url textbox changes but it would be stressful, so we only do it when the user didn't type 
        /// anything in the last 400ms. Once this timer expires, we do the check.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void delayQueryTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await TryUri(urlTextBox.Text);
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// When the users clicks the "..." button, we open a file selection dialog, then try to read the specified XML.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void fileButton_Click(object? sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog form = new OpenFileDialog())
                {
                    form.Title = @"Import character file";
                    form.Filter = @"CCP XML Character (*.xml)|*.xml";
                    form.FilterIndex = 0;

                    DialogResult dr = form.ShowDialog();
                    if (dr == DialogResult.Cancel)
                        return;

                    fileTextBox.Text = form.FileName;
                    fileRadio.Checked = true;
                    await TryUri(form.FileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Try to download a character XML from the given URI.
        /// </summary>
        /// <param name="uri"></param>
        private async Task TryUri(string uri)
        {
            m_version++;
            m_args = null!;

            // Update controls
            okButton.Enabled = false;
            namePanel.Visible = false;
            errorPanel.Visible = false;
            urlThrobber.State = ThrobberState.Rotating;

            // Starts querying the web or the hard drive, and invokes the given callback on result
            int version = m_version;

            if (string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri))
            {
                urlThrobber.State = ThrobberState.Stopped;
                okButton.Enabled = false;
                errorPanel.Visible = true;
                labelError.Text = @"Invalid URI format.";
                return;
            }

            var result = await GlobalCharacterCollection.TryAddOrUpdateFromUriAsync(parsedUri);
            if (version != m_version)
                return;

            urlThrobber.State = ThrobberState.Stopped;

            // Was there an error ?
            if (result.HasError)
            {
                okButton.Enabled = false;
                errorPanel.Visible = true;
                labelError.Text = result.Error;
                return;
            }

            nameTextBox.Text = result.CharacterName;
            namePanel.Visible = true;
            okButton.Enabled = true;
            m_args = result;
        }

        /// <summary>
        /// When the user clicks the OK button, we may have to generate the character
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void okButton_Click(object? sender, EventArgs e)
        {
            // Shouldn't occur but, well...
            if (m_args == null || m_args.HasError)
            {
                DialogResult = DialogResult.Cancel;
                return;
            }

            // Generates or updates the character
            if (m_uriCharacter == null)
                m_uriCharacter = m_args.CreateCharacter();
            else
                m_args.UpdateCharacter(m_uriCharacter);

            m_uriCharacter.Name = nameTextBox.Text;

            DialogResult = DialogResult.OK;
        }
    }
}