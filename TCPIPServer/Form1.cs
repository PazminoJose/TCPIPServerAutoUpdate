using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//********* namespaces Necesarios
using WatsonTcp;
using System.Net;
namespace TCPIPServer
{
    public partial class Form1 : Form
    {
        WatsonTcpServer server = null; // Declarar el servidor
        Dictionary<string, string> clientes = null; // Declarar el Diccionario para Clientes
        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (server != null) return; // Se retorna si ya existe un servidor
            // Activar/Desactivar Botones 
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnSend.Enabled = true;

            clientes = new Dictionary<string, string>(); // Crear nueva instancia para el diccionario de clientes

            txtStatus.Text += "Servidor Iniciado..." + "\r\n";
            string ip = IPAddress.Parse(txtHost.Text).ToString(); // obtner la ip para el host
            int port = Convert.ToInt32(txtPort.Text); // obtener el puerto para el host

            server = new WatsonTcpServer(ip, port); // Crear nueva instancia del servidor
            // Eventos que maeja WatsonTcp
            server.Events.ClientConnected += ClientConnected; // Se dispara al conectarse un cliente
            server.Events.ClientDisconnected += ClientDisconnected; // Se dispara al desconectarse un cliente
            server.Events.MessageReceived += MessageReceived; // Se dispara al Recibir un Mensaje de un cliente
            server.Callbacks.SyncRequestReceived = SyncRequestReceived; // Se dispara al recibir una petición Sincronica
            server.Start();
        }

        private void ClientConnected(object sender, ConnectionEventArgs args)
        {
            string ipClient = args.IpPort.Substring(0, args.IpPort.IndexOf(":"));
            if (!clientes.ContainsKey(ipClient))
            {
                clientes.Add(ipClient, args.IpPort);
            }
            string text = "Cliente Conectado: " + args.IpPort + "\r\n";
            changeStatus(text);
        }

        static void ClientDisconnected(object sender, DisconnectionEventArgs args)
        {
           // Console.WriteLine("Client disconnected: " + args.IpPort + ": " + args.Reason.ToString());
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            
            if (args.Metadata == null)
            {
                server.Send(args.IpPort, "Conectado al Servidor");
            }
            else
            {
                Dictionary<object, object> md = args.Metadata;
                string action = (string)md["action"];
                string type = (string)md["type"];
                switch (action)
                {
                    case "sendTxt":
                        sendText(type, md, args);
                        break;
                    case "sendImg":
                        sendImg(type, md, args);
                        break;
                }
            }
          
        }
        private void Broadcast(string msg, Dictionary<object, object> md)
        {

            if (server.ListClients().Count() > 0)
            {
                foreach (string ip in server.ListClients())
                {
                    string ips = ip;
                    server.Send(ip, msg, md);
                }
            }
            else
            {
                txtStatus.Text += "No Existen Clientes Conectados \r\n";
            }
        }
        private void Broadcast(byte[] data, Dictionary<object, object> md)
        {

            if (server.ListClients().Count() > 0)
            {
                foreach (string ip in server.ListClients())
                {
                    server.Send(ip, data, md);
                }
            }
            else
            {
                txtStatus.Text += "No Existen Clientes Conectados \r\n";
            }
        }
        private void sendText(string type, Dictionary<object, object> md, MessageReceivedEventArgs args)
        {
            Dictionary<object, object> mdSend = new Dictionary<object, object>();
            mdSend.Add("type", "text");
            if (type.Equals("one"))
            {

                string ipClient = clientes[(string)md["ipClient"]];
                string msg = Encoding.UTF8.GetString(args.Data);
                server.Send(ipClient, msg, mdSend);
                string text = "El cliente: " + args.IpPort + ": Envio un mensaje al cliente: " + ipClient + " con el contenido: " + Encoding.UTF8.GetString(args.Data) + "\r\n";
                changeStatus(text);
            }
            else if (type.Equals("broadcast"))
            {
                string text = "El cliente: " + args.IpPort + ": Envio un mensaje a todos los Clientes con el contenido: " + Encoding.UTF8.GetString(args.Data) + "\r\n";
                changeStatus(text);
                string msg = Encoding.UTF8.GetString(args.Data);
                Broadcast(msg, mdSend);
            }
        }
        private void sendImg(string type, Dictionary<object, object> md, MessageReceivedEventArgs args)
        {
            Dictionary<object, object> mdSend = new Dictionary<object, object>();
            mdSend.Add("type", "img");
            if (type.Equals("one"))
            {
                String ipClient = clientes[(String)md["ipClient"]];
                byte[] data = args.Data;
                server.Send(ipClient, data, mdSend);
                string text = "El cliente: " + args.IpPort + ": Envio una Imagen al Cliente: " + ipClient + "\r\n";
                changeStatus(text);

            }
            else if (type.Equals("broadcast"))
            {
                byte[] data = args.Data;
                Broadcast(data, mdSend);
                string text = "El cliente: " + args.IpPort + ": Envio una Imagen a Todos los Clientes \r\n";
                changeStatus(text);
            }
        }

            static SyncResponse SyncRequestReceived(SyncRequest req)
        {
            return new SyncResponse(req, "Conectado");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (server != null)
            {
                server.Stop();
                server = null;
                txtStatus.Clear();
            }

            txtStatus.Text = "Servidor Detenido";
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnSend.Enabled = false;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Dictionary<object, object> mdSend = new Dictionary<object, object>();
            mdSend.Add("type", "text");
            string msg = txtSend.Text;
            this.Broadcast(msg,mdSend);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnSend.Enabled = false;
        }
        private void changeStatus(string text)
        {
            txtStatus.Invoke((MethodInvoker)delegate ()
            {
                txtStatus.Text += text;

            });
        }
    }
}
