using System.Windows.Forms;

namespace BobRfid
{
    public partial class SettingsForm : Form
    {
        public SettingsForm(object settings)
        {
            InitializeComponent();
            SettingsPropertyGrid.SelectedObject = settings;
        }
    }
}
