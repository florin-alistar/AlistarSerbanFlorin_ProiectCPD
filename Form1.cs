using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dezbateri
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            mappingBetweenPublishButtonAndInputWithTopicName = new Dictionary<Button, (TextBox inputField, string discussionTopicName)>
            {
                [buttonPubSportMsg] = (textBoxPubSportMsg, "Sport"),
                [buttonPubEducMsg] = (textBoxEducPubMsg, "Educatie"),
                [buttonPubPoliticsMsg] = (textBoxPoliticsMsg, "Politica"),
                [buttonPubTechMsg] = (textBoxTechMsgs, "Tehnologie")
            };
            buttonPubSportMsg.Click += PublishMessageButtonClicked;
            buttonPubEducMsg.Click += PublishMessageButtonClicked;
            buttonPubPoliticsMsg.Click += PublishMessageButtonClicked;
            buttonPubTechMsg.Click += PublishMessageButtonClicked;
            ShowOrHidePublishDisplay(false);

            topicNameAndOutputListBox = new Dictionary<string, ListBox>
            {
                ["Sport"] = listBoxSubSport,
                ["Educatie"] = listBoxSubEducation,
                ["Politica"] = listBoxSubPolitica,
                ["Tehnologie"] = listBoxSubTehnologie
            };

            dateUtilizator.RabbitMQHandler.MessageReceivedHandler += this.MessageReceived;


            // modif noi -> procesele aleg liderul singure
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.LeaveTokenHandler += LeaveToken;
        }

        private Dictionary<string, List<string>> sentMessages = new Dictionary<string, List<string>>
        {
            ["Sport"] = new List<string>(),
            ["Politica"] = new List<string>(),
            ["Educatie"] = new List<string>(),
            ["Tehnologie"] = new List<string>()
        };

        private void MessageReceived(string msg)
        {
            string[] parts = msg.Split('@');
            string poster = parts[0];
            string topic = parts[1];
            string message = parts[2];
            this.Invoke((MethodInvoker)delegate
            {
                ListBox subTopic = topicNameAndOutputListBox[topic];
                subTopic.Items.Add($"{poster}: {message}");
            });
        }

        // fara @ in mesaj!
        private void PublishMessageButtonClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            var pair = mappingBetweenPublishButtonAndInputWithTopicName[button];
            string text = pair.inputField.Text;
            if (String.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Mesaj gol nepermis");
            }
            else if (text.Contains("@"))
            {
                MessageBox.Show("@ este interzis intr-un mesaj");
            }
            else
            {
                dateUtilizator.SendMessage(pair.discussionTopicName, text);
                pair.inputField.Text = String.Empty;
                sentMessages[pair.discussionTopicName].Add(text);
            }

        }

        private void ButtonSetareParametri_Click(object sender, EventArgs e)
        {

            buttonGrabToken.Enabled = buttonBeginWaitingForToken.Enabled = false;

            string username = textBoxUsername.Text;
            if (String.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Nume gol");
                return;
            }

            int tokenDuration = int.Parse(textBoxDurataToken.Text);
            int entryPort = int.Parse(textBoxPortIntrare.Text);
            int exitPort = int.Parse(textBoxPortIesire.Text);
            if (dateUtilizator.PublishTopics.Count < 2)
            {
                MessageBox.Show("Fiecare persoana trebuie sa faca publish la cel putin 2 topicuri!");
                return;
            }

            if (dateUtilizator.SubscribeTopics.Count < 2)
            {
                MessageBox.Show("Fiecare persoana trebuie sa faca subscribe la cel putin 2 topicuri!");
                return;
            }

            dateUtilizator.Name = username;
            dateUtilizator.TotalTokenDuration = tokenDuration;
            dateUtilizator.LeftTokenDuration = tokenDuration;
            dateUtilizator.EntryPort = entryPort;
            dateUtilizator.ExitPort = exitPort;

            buttonGrabToken.Enabled = buttonBeginWaitingForToken.Enabled = true;
            dateUtilizator.CloseSubscriptions();
            dateUtilizator.SubscribeToSubscriptionTopics();

            // ** Functia de pornire a socket-urilor
            tokenHandler.InitializeSockets(entryPort, exitPort, dateUtilizator.Name, checkBoxBeginElectionAlg.Checked);
        }

        private DateUtilizator dateUtilizator = new DateUtilizator();
        private TokenHandler tokenHandler;

        private Dictionary<Button, (TextBox inputField, string discussionTopicName)>
            mappingBetweenPublishButtonAndInputWithTopicName;
        private Dictionary<string, ListBox> topicNameAndOutputListBox;

        private void ButtonAddTopicToYourSub_Click(object sender, EventArgs e)
        {
            int index = listBoxAllTopicsSubscription.SelectedIndex;
            if (index != -1)
            {
                string topic = listBoxAllTopicsSubscription.Items[index].ToString();
                dateUtilizator.SubscribeTopics.Add(topic);
                listBoxYourSubscriptionTopics.Items.Add(topic);
                listBoxAllTopicsSubscription.Items.RemoveAt(index);

                ControlSubscribeDisplay();
            }
        }

        private void ButtonRemoveTopicFromYourSub_Click(object sender, EventArgs e)
        {
            int index = listBoxYourSubscriptionTopics.SelectedIndex;
            if (index != -1)
            {
                string topic = listBoxYourSubscriptionTopics.Items[index].ToString();
                dateUtilizator.SubscribeTopics.Remove(topic);
                listBoxAllTopicsSubscription.Items.Add(topic);
                listBoxYourSubscriptionTopics.Items.RemoveAt(index);

                ControlSubscribeDisplay();
            }
        }

        private void ButtonAddPubTopic_Click(object sender, EventArgs e)
        {
            int index = listBoxAllTopicsPub.SelectedIndex;
            if (index != -1)
            {
                string topic = listBoxAllTopicsPub.Items[index].ToString();
                dateUtilizator.PublishTopics.Add(topic);
                listBoxYourPubTopics.Items.Add(topic);
                listBoxAllTopicsPub.Items.RemoveAt(index);

                ControlPublishDisplay();
            }
        }

        private void ButtonRemovePubTopic_Click(object sender, EventArgs e)
        {
            int index = listBoxYourPubTopics.SelectedIndex;
            if (index != -1)
            {
                string topic = listBoxYourPubTopics.Items[index].ToString();
                dateUtilizator.PublishTopics.Remove(topic);
                listBoxAllTopicsPub.Items.Add(topic);
                listBoxYourPubTopics.Items.RemoveAt(index);

                ControlPublishDisplay();
            }
        }

        // afisam doar acele ferestre pe care le-a ales utilizatorul pentru publish
        private void ControlPublishDisplay()
        {
            groupBoxSportPublish.Visible = this.dateUtilizator.PublishTopics.Contains("Sport");
            groupBoxEducatiePublish.Visible = this.dateUtilizator.PublishTopics.Contains("Educatie");
            groupBoxPoliticsPublish.Visible = this.dateUtilizator.PublishTopics.Contains("Politica");
            groupBoxTechnologyPublish.Visible = this.dateUtilizator.PublishTopics.Contains("Tehnologie");
        }

        // cand nu avem token-ul, dezactivam controalele de scriere
        // cand il primim, le reactivam
        private void ShowOrHidePublishDisplay(bool show)
        {
            groupBoxSportPublish.Enabled = show;
            groupBoxEducatiePublish.Enabled = show;
            groupBoxPoliticsPublish.Enabled = show;
            groupBoxTechnologyPublish.Enabled = show;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            dateUtilizator.LeftTokenDuration--;
            if (dateUtilizator.LeftTokenDuration == 0)
            {
                //TokenHandler.SendToken(dateUtilizator.ExitPort);

                // ** Ni s-a terminat timpul de pastrare a token-ului, deci il trimitem la vecinul din dreapta
                tokenHandler.SendMessageToNextProcess("token");

                timer1.Enabled = false;
                ShowOrHidePublishDisplay(false);
                labelHasToken.Text = "Waiting for token...";
            }
            else
            {
                labelHasToken.Text = "Token left: " + dateUtilizator.LeftTokenDuration + " s";
            }
        }

        // afisare ferestre pentru subscribe
        private void ControlSubscribeDisplay()
        {
            groupBoxSubSport.Visible = dateUtilizator.SubscribeTopics.Contains("Sport");
            groupBoxSubEducatie.Visible = dateUtilizator.SubscribeTopics.Contains("Educatie");
            groupBoxSubPolitica.Visible = dateUtilizator.SubscribeTopics.Contains("Politica");
            groupBoxSubTehnologie.Visible = dateUtilizator.SubscribeTopics.Contains("Tehnologie");
        }

        private void ButtonGrabToken_Click(object sender, EventArgs e)
        {
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.ListenForToken(dateUtilizator.EntryPort);
            labelHasToken.Text = "Token left: " + dateUtilizator.LeftTokenDuration + " s";
            timer1.Enabled = true;
            ShowOrHidePublishDisplay(true);
        }

        private void ButtonBeginWaitingForToken_Click(object sender, EventArgs e)
        {
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.ListenForToken(dateUtilizator.EntryPort);
            ShowOrHidePublishDisplay(false);
            labelHasToken.Text = "Waiting for token...";
        }

        // Am primit token -> modif UI
        private void TokenReceivedHandler()
        {
            dateUtilizator.LeftTokenDuration = dateUtilizator.TotalTokenDuration;   
            this.Invoke((MethodInvoker)delegate
            {
                ShowOrHidePublishDisplay(true);
                labelHasToken.Text = "Token left: " + dateUtilizator.LeftTokenDuration + " s";
                timer1.Enabled = true;
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            dateUtilizator.CloseSubscriptions();
            tokenHandler.CloseSockets();
        }

        private void ButtonViewSentMessages_Click(object sender, EventArgs e)
        {
            FormMessages formMessages = new FormMessages(dateUtilizator.Name, this.sentMessages);
            formMessages.Show();
        }



        private void LeaveToken()
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowOrHidePublishDisplay(false);
                labelHasToken.Text = "Waiting for token...";
                timer1.Enabled = false;
            });
        }
    }
}
