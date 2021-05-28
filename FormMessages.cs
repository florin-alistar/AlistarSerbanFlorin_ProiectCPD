using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dezbateri
{
    // Aici se pot vedea toate mesajele transmise de un proces
    public partial class FormMessages : Form
    {
        public FormMessages()
        {
            InitializeComponent();
        }

        public FormMessages(string username, Dictionary<string, List<string>> messagesSent)
        {
            InitializeComponent();
            InitData(username, messagesSent);
        }

        private void InitData(string username, Dictionary<string, List<string>> messagesSent)
        {
            this.labelNume.Text = username;
            List<string> sports = messagesSent["Sport"];
            List<string> politics = messagesSent["Politica"];
            List<string> education = messagesSent["Educatie"];
            List<string> technology = messagesSent["Tehnologie"];
            listBoxSport.Items.AddRange(sports.ToArray());
            listBoxPolitica.Items.AddRange(politics.ToArray());
            listBoxEducatie.Items.AddRange(education.ToArray());
            listBoxTehnologie.Items.AddRange(technology.ToArray());
        }
    }
}
