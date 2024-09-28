
namespace custom_folder_browser
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            using (var dlg = new CustomFolder.CustomFolderBrowser())
            {
                //dlg.Font = new Font(Font.FontFamily, 20F, FontStyle.Regular);
                dlg.ShowDialog();
                BeginInvoke(() => Close());
            }
        }
    }
}
