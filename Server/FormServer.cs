using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace Server
{
    public partial class FormServer : Form
    {

        //private int listenport = 5555;
        private Socket listener;
        private ArrayList arraylist_cli;
        //private Thread processor;
        private Socket clientsocket;
        //private Thread clientservice;
        delegate void SetTextCallback(Client c, string ev);

        public FormServer()
        {
            InitializeComponent();
        }

        private void FormServer_Load(object sender, EventArgs e)
        {
            //this.WindowState = FormWindowState.Minimized;
            arraylist_cli = new ArrayList();
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Visible = false;

            
        }
        private void StartListening()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(txtServidor.Text);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, int.Parse(txtPuerto.Text));

                //listener = new TcpListener(listenport);
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(10);
                while (true)
                {
                    try
                    {
                        Socket s = listener.Accept();
                        clientsocket = s;
                        Task task = new Task(() => ServiceClient());
                        task.Start();

                    }
                    catch (Exception ew)
                    {
                        MessageBox.Show("Messege: " + ew.Message + " \n Error: " + ew.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Messege: " + ex.Message + " \n Error: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ServiceClient()
        {
            Socket client = clientsocket;
            bool keepalive = true;

            while (keepalive)
            {
                try
                {
                    Byte[] buffer = new Byte[1024];
                    client.Receive(buffer);
                    string clientcommand = System.Text.Encoding.UTF8.GetString(buffer);

                    string[] tokens = clientcommand.Split(new Char[] { '|' });
                    Console.WriteLine(clientcommand);

                    if (tokens[0] == "CONN")
                    {
                        for (int n = 0; n < arraylist_cli.Count; n++)
                        {
                            Client cl = (Client)arraylist_cli[n];
                            SendToClient(cl, "JOIN|" + tokens[1]);
                        }
                        EndPoint ep = client.RemoteEndPoint;
                        //string add = ep.ToString();
                        Client c = new Client(tokens[1], ep, client);
                        arraylist_cli.Add(c);
                        string message = "LIST|" + GetChatterList() + "\r\n";
                        SendToClient(c, message);
                        this.SetText(c, "Add");

                    }
                    if (tokens[0] == "CHAT")
                    {
                        for (int n = 0; n < arraylist_cli.Count; n++)
                        {
                            Client cl = (Client)arraylist_cli[n];
                            SendToClient(cl, clientcommand);
                        }
                    }
                    if (tokens[0] == "PRIV")
                    {
                        string destclient = tokens[3];
                        for (int n = 0; n < arraylist_cli.Count; n++)
                        {
                            Client cl = (Client)arraylist_cli[n];
                            if (cl.Name.CompareTo(tokens[3]) == 0)
                                SendToClient(cl, clientcommand);
                            if (cl.Name.CompareTo(tokens[1]) == 0)
                                SendToClient(cl, clientcommand);
                        }
                    }
                    if (tokens[0] == "GONE")
                    {
                        int remove = 0;
                        bool found = false;
                        int c = arraylist_cli.Count;
                        for (int n = 0; n < c; n++)
                        {
                            Client cl = (Client)arraylist_cli[n];
                            SendToClient(cl, clientcommand);
                            if (cl.Name.CompareTo(tokens[1]) == 0)
                            {
                                remove = n;
                                found = true;
                                SetText(cl, "remove");
                            }
                        }
                        if (found)
                            arraylist_cli.RemoveAt(remove);
                        client.Close();
                        keepalive = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Messege: " + ex.Message + " \n Error: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void SetText(Client text, string ev)
        {
            if (this.lbClients.InvokeRequired)//Si InvokeRequired devuelve true, llame a Invoke con un delegado que realice la llamada real al control.
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text, ev });
            }
            else
            {//Si InvokeRequired devuelve false, llame directamente al control.
                if (ev == "remove")
                {
                    lbClients.Items.Remove(text);
                }
                else
                {
                    lbClients.Items.Add(text);
                }
                //this.textBox1.Text = text;
            }
        }

        private void SendToClient(Client cl, string message)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message.ToCharArray());
                cl.Sock.Send(buffer, buffer.Length, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Messege: " + ex.Message + " \n Error: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cl.Sock.Close();
                arraylist_cli.Remove(cl);
                lbClients.Items.Remove(cl.Name + " : " + cl.Host.ToString());
            }
        }
        private string GetChatterList()
        {
            string chatters = "";
            for (int n = 0; n < arraylist_cli.Count; n++)
            {
                Client cl = (Client)arraylist_cli[n];
                chatters += cl.Name;
                chatters += "|";
            }
            chatters.Trim(new char[] { '|' });
            return chatters;
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (BtnConnect.Text == "Iniciar")
            {
                toolStripProgressBar1.Visible = true;
                toolStripStatusLabel1.Visible = true;
                BtnConnect.Text = "Parar";
                Task task = new Task(() => StartListening());
                task.Start();
            }else
            {
                BtnConnect.Text = "Iniciar";
                Stop_Server();
            }
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(500, "Information", "Servidor iniciado correctamente.", ToolTipIcon.Info);
            
        }

        private void FormServer_FormClosing(object sender, FormClosingEventArgs e)
        {

            switch (e.CloseReason) // Evento que contiene la acción del cerrado
            {
                case CloseReason.None: //No se definió la causa del cierre o no se pudo determinar.
                    e.Cancel = false;
                    Stop_Server();
                    break;
                case CloseReason.WindowsShutDown: //El sistema operativo está cerrando todas las aplicaciones antes de cerrarse.
                    e.Cancel = false;
                    Stop_Server();
                    break;
                case CloseReason.MdiFormClosing: //El formulario principal de este formulario de interfaz de múltiples documentos (MDI) está cerrándose.
                    e.Cancel = true;
                    break;
                case CloseReason.UserClosing: //El usuario está cerrando el formulario a través de la interfaz de usuario (IU), ventana o presionando ALT+F4.
                    e.Cancel = true;
                    break;
                case CloseReason.TaskManagerClosing: //El Administrador de tareas de Microsoft Windows está cerrando la aplicación.
                    Stop_Server();
                    e.Cancel = false;
                    break;
                case CloseReason.FormOwnerClosing: //Que el formulario de propietario está cerrándose.
                    e.Cancel = true;
                    break;
                case CloseReason.ApplicationExitCall:  //Invocación del método Exit de la clase Application.
                    Stop_Server();
                    e.Cancel = false;
                    break;
            }
            Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(500, "Information", "Escuchando conexiones!", ToolTipIcon.Info);
        }
        private void Stop_Server()
        {
            try
            {
                for (int n = 0; n < arraylist_cli.Count; n++)
                {
                    Client cl = (Client)arraylist_cli[n];
                    SendToClient(cl, "QUIT|");
                    cl.Sock.Close();
                }
                
            }
            catch (ThreadAbortException ex)
            {
                MessageBox.Show("Messege: " + ex.Message + " \n Error: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormServer_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            } 
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
           
        }

        private void miAbrir_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;  
        }

        private void miReiniciar_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}
