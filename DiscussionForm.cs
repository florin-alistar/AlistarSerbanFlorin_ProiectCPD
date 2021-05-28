using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dezbateri
{
    public partial class DiscussionForm : Form
    {
        public DiscussionForm()
        {
            InitializeComponent();

            InitDictionaries();

            buttonPubSportMsg.Click += PublishMessageButtonClicked;
            buttonPubEducMsg.Click += PublishMessageButtonClicked;
            buttonPubPoliticsMsg.Click += PublishMessageButtonClicked;
            buttonPubTechMsg.Click += PublishMessageButtonClicked;
            ShowPublishDisplay(false);

            processData.RabbitMQHandler.MessageReceivedHandler += this.MessageReceived;


            // modif noi -> procesele aleg liderul singure
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.LeaveTokenHandler += LeaveToken;
        }

        private void InitDictionaries()
        {
            publishButtonToInputTopicMap = new Dictionary<Button, (TextBox inputField, string discussionTopicName)>
            {
                [buttonPubSportMsg] = (textBoxPubSportMsg, "Sport"),
                [buttonPubEducMsg] = (textBoxEducPubMsg, "Educatie"),
                [buttonPubPoliticsMsg] = (textBoxPoliticsMsg, "Politica"),
                [buttonPubTechMsg] = (textBoxTechMsgs, "Tehnologie")
            };

            topicNameAndOutputListBox = new Dictionary<string, ListBox>
            {
                ["Sport"] = listBoxSubSport,
                ["Educatie"] = listBoxSubEducation,
                ["Politica"] = listBoxSubPolitica,
                ["Tehnologie"] = listBoxSubTehnologie
            };

            sentMessages = new Dictionary<string, List<string>>
            {
                ["Sport"] = new List<string>(),
                ["Politica"] = new List<string>(),
                ["Educatie"] = new List<string>(),
                ["Tehnologie"] = new List<string>()
            };
        }

        private Dictionary<string, List<string>> sentMessages;
        private ProcessData processData = new ProcessData();
        private TokenHandler tokenHandler;

        private Dictionary<Button, (TextBox inputField, string discussionTopicName)> publishButtonToInputTopicMap;
        private Dictionary<string, ListBox> topicNameAndOutputListBox;

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
            var pair = publishButtonToInputTopicMap[button];
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
                processData.SendMessage(pair.discussionTopicName, text);
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
            if (processData.PublishTopics.Count < 2)
            {
                MessageBox.Show("Fiecare persoana trebuie sa faca publish la cel putin 2 topicuri!");
                return;
            }

            if (processData.SubscribeTopics.Count < 2)
            {
                MessageBox.Show("Fiecare persoana trebuie sa faca subscribe la cel putin 2 topicuri!");
                return;
            }

            processData.Name = username;
            processData.TotalTokenDuration = tokenDuration;
            processData.LeftTokenDuration = tokenDuration;
            processData.EntryPort = entryPort;
            processData.ExitPort = exitPort;

            buttonGrabToken.Enabled = buttonBeginWaitingForToken.Enabled = true;
            processData.CloseSubscriptions();
            processData.SubscribeToSubscriptionTopics();

            // ** Functia de pornire a socket-urilor
            tokenHandler.InitializeSockets(entryPort, exitPort, processData.Name, checkBoxBeginElectionAlg.Checked);
        }

        private void ButtonAddTopicToYourSub_Click(object sender, EventArgs e)
        {
            int index = listBoxAllTopicsSubscription.SelectedIndex;
            if (index != -1)
            {
                string topic = listBoxAllTopicsSubscription.Items[index].ToString();
                processData.SubscribeTopics.Add(topic);
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
                processData.SubscribeTopics.Remove(topic);
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
                processData.PublishTopics.Add(topic);
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
                processData.PublishTopics.Remove(topic);
                listBoxAllTopicsPub.Items.Add(topic);
                listBoxYourPubTopics.Items.RemoveAt(index);

                ControlPublishDisplay();
            }
        }

        // afisam doar acele ferestre pe care le-a ales utilizatorul pentru publish
        private void ControlPublishDisplay()
        {
            groupBoxSportPublish.Visible = this.processData.PublishTopics.Contains("Sport");
            groupBoxEducatiePublish.Visible = this.processData.PublishTopics.Contains("Educatie");
            groupBoxPoliticsPublish.Visible = this.processData.PublishTopics.Contains("Politica");
            groupBoxTechnologyPublish.Visible = this.processData.PublishTopics.Contains("Tehnologie");
        }

        // cand nu avem token-ul, dezactivam controalele de scriere
        // cand il primim, le reactivam
        private void ShowPublishDisplay(bool show)
        {
            groupBoxSportPublish.Enabled = show;
            groupBoxEducatiePublish.Enabled = show;
            groupBoxPoliticsPublish.Enabled = show;
            groupBoxTechnologyPublish.Enabled = show;
        }

        // afisare ferestre pentru subscribe
        private void ControlSubscribeDisplay()
        {
            groupBoxSubSport.Visible = processData.SubscribeTopics.Contains("Sport");
            groupBoxSubEducatie.Visible = processData.SubscribeTopics.Contains("Educatie");
            groupBoxSubPolitica.Visible = processData.SubscribeTopics.Contains("Politica");
            groupBoxSubTehnologie.Visible = processData.SubscribeTopics.Contains("Tehnologie");
        }

        private void ButtonGrabToken_Click(object sender, EventArgs e)
        {
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.ListenForToken(processData.EntryPort);
            labelHasToken.Text = "Token left: " + processData.LeftTokenDuration + " s";
            timerKeepTokenCounter.Enabled = true;
            ShowPublishDisplay(true);
        }

        private void ButtonBeginWaitingForToken_Click(object sender, EventArgs e)
        {
            tokenHandler = new TokenHandler();
            tokenHandler.TokenReceivedHandler += TokenReceivedHandler;
            tokenHandler.ListenForToken(processData.EntryPort);
            ShowPublishDisplay(false);
            labelHasToken.Text = "Waiting for token...";
        }

        // Am primit token -> modif UI
        private void TokenReceivedHandler()
        {
            processData.LeftTokenDuration = processData.TotalTokenDuration;   
            this.Invoke((MethodInvoker)delegate
            {
                ShowPublishDisplay(true);
                labelHasToken.Text = "Token left: " + processData.LeftTokenDuration + " s";
                timerKeepTokenCounter.Enabled = true;
            });
        }

        private void ButtonViewSentMessages_Click(object sender, EventArgs e)
        {
            FormMessages formMessages = new FormMessages(processData.Name, this.sentMessages);
            formMessages.Show();
        }



        private void LeaveToken()
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowPublishDisplay(false);
                labelHasToken.Text = "Waiting for token...";
                timerKeepTokenCounter.Enabled = false;
            });
        }

        private void TimerKeepTokenCounter_Tick(object sender, EventArgs e)
        {
            processData.LeftTokenDuration--;
            if (processData.LeftTokenDuration == 0)
            {
                //TokenHandler.SendToken(processData.ExitPort);

                // ** Ni s-a terminat timpul de pastrare a token-ului, deci il trimitem la vecinul din dreapta
                tokenHandler.SendMessageToNextProcess("token");

                timerKeepTokenCounter.Enabled = false;
                ShowPublishDisplay(false);
                labelHasToken.Text = "Waiting for token...";
            }
            else
            {
                labelHasToken.Text = "Token left: " + processData.LeftTokenDuration + " s";
            }
        }

        private void DiscussionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            processData.CloseSubscriptions();
            tokenHandler.CloseSockets();
        }
    }
}
