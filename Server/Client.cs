using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Server
{
    public class Client
    {
        private EndPoint endpoint;
        private string name;
        private Socket sock;
        public Client(string _name, EndPoint _endpoint, Socket _sock)
        {
            //port = Convert.ToInt32(_port);
            //clthread = _thread;
            endpoint = _endpoint;
            name = _name;
            sock = _sock;
        }

        public override string ToString()
        {
            return endpoint.ToString() + " : " + name;
        }

        //public Thread CLThread
        //{
        //    get{return clthread;}
        //    set{clthread = value;}
        //}
        public EndPoint Host
        {
            get { return endpoint; }
            set { endpoint = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public Socket Sock
        {
            get { return sock; }
            set { sock = value; }
        }
    }
}
