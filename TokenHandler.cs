using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dezbateri
{
    public class TokenHandler
    {
        public event Action TokenReceivedHandler;
        public event Action LeaveTokenHandler;

        private Socket entrySocket;
        private Socket exitSocket;

        public void CloseSockets()
        {
            try
            {
                exitSocket?.Close();
            }
            catch { }

            try
            {
                entrySocket?.Close();
            }
            catch { }
        }

        public void InitializeSockets(int entryPort, int exitPort, string ownName, bool beingElectionAlg = false)
        {
            // Creare socket de iesire (prin asta se trimit mesaje catre urmatorul proces)
            Task.Run(() => CreateOutSocket(exitPort, beingElectionAlg, ownName));

            Task.Run(() =>
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, entryPort);
                entrySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                entrySocket.Bind(endPoint);
                entrySocket.Listen(5);

                // avem numai un client conectat (vecinul din stanga)
                //      -> deci daca s-a facut conexiunea o data, atunci nu mai are rost sa asteptam dupa alte conexiuni
                Socket client = entrySocket.Accept();
                // trecem la asteptarea doar dupa mesaje de la clientul conectat
                ProcessConnectedSocket(client, ownName);
            });
        }

        private void ProcessConnectedSocket(Socket client, string ownName)
        {
            while (true)
            {
                byte[] data = new byte[100];
                int recBytes = client.Receive(data);
                string msg = Encoding.UTF8.GetString(data).Trim('\0');
                // I-a venit randul acestui proces sa tina token-ul
                //      "TokenReceivedHandler" e legat de o metoda care sa actualizeze UI -> metoda cu acelasi nume din Form1.cs
                if (msg.Equals("token"))
                {
                    TokenReceivedHandler?.Invoke();
                }
                else
                {
                    // Avem un mesaj specific procesului de alegere a detinatorului token-ului
                    ProcessElectionMessage(msg, ownName);
                }
            }
        }

        // Algoritm de alegere in doua etape:
        //    1) Fiecare proces scrie in mesaj numele sau ("ownName")
        //       DUPA CE toate procesele au terminat 1), toate procesele fac pasul urmator:
        //    2) Cand toata procesele si-au scris numele, atunci ele decid daca sunt detinatori sau nu
        //              pe baza ordinii alfabetice a numelor; marcheaza acest lucru in mesaj prin incrementarea unei variabile
        // Desigur, dupa pasul 1) si dupa pasul 2) executat de fiecare proces, acesta trimite mesajul actualizat vecinului din dreapta
        //        , 1) si 2) se fac pe rand (nu deodata de fiecare proces)
        private void ProcessElectionMessage(string msg, string ownName)
        {
            // nume_proces_1_care_a_pus_numele,nume_proces_2_care_a_pus_numele,...@nr_procese_care_au_vazut_daca_iau_tokenul_sau_asteapta
            string[] parts = msg.Split('@');
            List<string> processesThatDidStep1 = parts[0].Split(new char[] { ',' }).ToList();
            int noProcessesThatDidStep2 = int.Parse(parts[1]);

            // Toate procesele au vazut care e rolul lor (daca iau tokenul sau daca asteapta dupa el), deci nu mai facem nimic
            // !!! ODATA CE S-A AJUNS AICI, ELECTIA SE TERMINA
            //  FUNCTIA ACEASTA NU MAI ARE TREABA CU PASAREA ULTERIOARA A TOKEN-ULUI DE LA PROCES LA PROCES
            if (processesThatDidStep1.Count != 0 && processesThatDidStep1.Count == noProcessesThatDidStep2)
            {
                return;
            }

            // Daca toate procesele si-au pus numele lor in mesaj, atunci putem aplica logica pentru a determina castigatorul token-ului
            //          (-> cand avem in lista1 numele procesului curent)
            // Castigatorul e cel cu numele in ordine alfabetica primul; restul vor astepta
            // fiecare proces va face asta (desigur, pe rand) si isi va lua astfel starea
            if (processesThatDidStep1.Contains(ownName))
            {
                IEnumerable<string> orderedNames = processesThatDidStep1.OrderBy(n => n);
                // castigatorul token-ului
                if (ownName.Equals(orderedNames.First()))
                {
                    TokenReceivedHandler?.Invoke();
                }
                else
                {
                    LeaveTokenHandler?.Invoke();
                }
                // acest proces si-a terminat logica de token, deci marcheaza acest lucru
                //      incrementand cu 1 numarul proceselor care au facut asta
                parts[parts.Length - 1] = (noProcessesThatDidStep2 + 1).ToString();
            }
            // Daca ajungem aici inseamna ca acest proces nu si-a pus numele in mesaj (pentru ca avem un inel si un singur mesaj),
            //  deci trebuie sa o faca acum
            else
            {
                // aici suntem la pasul 1), deci niciun proces nu poate face inca logica pt token --> indicam acest lucru prin ultimul parametru, "0"
                processesThatDidStep1.Add(ownName);
                parts[0] = string.Join(",", processesThatDidStep1);
                parts[1] = "0";
            }

            // Trimitem mesajul la urmatorul proces
            //  atat timp cat algoritmul de alegere nu e gata
            SendMessageToNextProcess(string.Join("@", parts));
        }

        public void SendMessageToNextProcess(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            exitSocket.Send(data);
        }

        private void CreateOutSocket(int exitPort, bool beginElectionAlg, string ownName)
        {
            bool failed = true;
            // pentru ca procesul urmator ar putea sa nu asculte pe portul de intrare
            //      in acest moment, trebuie sa incercam sa ne conectam pana reusim
            //          (pana blocul try se executa in intregime)
            while (failed)
            {
                try
                {
                    exitSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, exitPort);
                    exitSocket.Connect(remoteEP);
                    failed = false;
                }
                catch { }
            }

            // Aici e o problema - procesele participa la un proces de alegere a detinatorului initial al token-ului,
            //      care se va acorda aceluia cu numele de utilizator primul in ordine alfabetica
            // dar care proces va incepe procesul de alegere (cred ca cel mai bine e sa fie unul singur - asa am facut)?
            //      se va specifica asta din UI printr-un checkbox

            //  Algoritmul va functiona doar daca toate procesele au socket-urile de intrare si de iesire pornite
            //    atunci cand se da drumul la algoritm
            //  Daca avem ---50---> P1 ---60---> P2 ---70---> P3 ---50---> P1....
            //      si zicem ca P1 va porni algoritmul de alegere, trebuie ca P3 si P2 sa aiba socket-urile de intrare si de iesire pornite!
            //          (si P1 bineinteles)
            //  => daca se incearca rularea acestui exemplu, atunci ordinea cea mai buna de pornire este P3, P2, P1
            //          (pornirea inseamna de fapt apasarea butonului "Setare")


            // asteptam putin pana sa incepem algoritmul de alegere
            // Aici eventual am putea pune un scheduler care sa reporneasca alg o data la 100 de secunde sau asa ceva
            //      dar acum facem algoritmul numai la inceput, numai o data
            Thread.Sleep(1500);
            if (beginElectionAlg)
            {
                // Cand incepem algoritmul, punem procesul curent in lista celor care au facut pasul 1
                SendMessageToNextProcess($"{ownName}@0");
            }
        }



        // impl veche
        public static void SendToken(int exitPort)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, exitPort);
            socket.Connect(remoteEP);
            byte[] data = Encoding.UTF8.GetBytes("token");
            socket.Send(data);
            socket.Disconnect(false);
            socket.Close();
        }

        public void ListenForToken(int entryPort)
        {
            Task.Run(() =>
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, entryPort);
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endPoint);
                listener.Listen(5);
                while (true)
                {
                    Socket handler = listener.Accept();
                    byte[] data = new byte[50];
                    int bytesRec = handler.Receive(data);
                    string msg = Encoding.UTF8.GetString(data, 0, bytesRec);
                    if ("token".Equals(msg.ToLower()))
                    {
                        TokenReceivedHandler?.Invoke();
                    }
                }
            });
        }
    }
}
