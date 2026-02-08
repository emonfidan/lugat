using System.Windows;

namespace LugatimApp
{
    public partial class NoteDialog : Window
    {
        public string NoteText { get; private set; }

        public NoteDialog(string wordName, string existingNote)
        {
            InitializeComponent();

            txtWordTitle.Text = $"'{wordName}' için not";
            txtNote.Text = existingNote ?? "";
            txtNote.Focus();
            txtNote.SelectAll();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            NoteText = txtNote.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
