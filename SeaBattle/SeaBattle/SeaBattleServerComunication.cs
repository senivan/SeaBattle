﻿using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeaBattle;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Controls;
using SeaBattle.UserControls;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using ShipsClass;

namespace SeaBattleServerComunication
{
    #region Server
    class ServerConnection
    {
        private const int port = 8008;
        private const string hostname = "26.246.103.91";
        //26.246.103.91

        public static TcpClient client = null;
        public static NetworkStream NetStream = null;

        public static void Connect()
        {
            try
            {
                client = new TcpClient(hostname, port);
                NetStream = client.GetStream();
                Task.Run(() =>
                {
                    ReceiveFromServer.Listen();
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Server is down...");
            }
        }
    }
    #endregion
    #region ServerComunication
    class ReceiveFromServer
    {
        public static void Listen()
        {
            //try
            //{
            while (true)
            {
                StringBuilder builder = new StringBuilder();

                byte[] data = new byte[256];
                do
                {
                    int bytes = ServerConnection.NetStream.Read(data, 0, data.Length);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (ServerConnection.NetStream.DataAvailable);

                Request request = JsonConvert.DeserializeObject<Request>(builder.ToString());
                if (request == null)
                {
                    continue;
                }
                switch (request.ReqType)
                {
                    case RequestType.Login:
                        {
                            if (request.Data[0] == true.ToString())
                            {
                                ChangeUserControler.ToFriends();
                            }
                            else
                            {
                                UC_LoginPage.showException.Invoke("Wrong login or password!");
                            }
                            break;
                        }
                    case RequestType.Register:
                        {
                            if (request.Data[0] != true.ToString())
                            {
                                MessageBox.Show(request.Data[0]);
                            }
                            else
                            {
                                if (request.Data.Count != 1)
                                {
                                    if (request.Data[1] == true.ToString())
                                    {
                                        MessageBox.Show("Successfully registered!");
                                        ChangeUserControler.ToLoginPage();

                                    }
                                    else
                                    {
                                        MessageBox.Show(request.Data[1]);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Code sended on your email!");
                                    ChangeUserControler.EnableCodeTextBox();
                                }
                            }
                            break;
                        }
                    case RequestType.GetRewards:
                        {
                            ChangeUserControler.ShowRewards(request);
                            break;
                        }
                    case RequestType.AddFriend:
                        {
                            if (request.Data[0] == "User added to friends!")
                            {
                                SendToServer.SendFriendsRequest();
                            }
                            MessageBox.Show(request.Data[0]);
                            break;
                        }
                    case RequestType.GetFriends:
                        {
                            ChangeUserControler.ShowFriends(request);
                            break;
                        }
                    case RequestType.BattleRequest:
                        {
                            if (request.Login == null)
                            {
                                ChangeUserControler.CancelBattle();
                                MessageBox.Show(request.Data[0]);
                            }
                            else
                            {
                                ChangeUserControler.ToAcceptBattle(request);
                            }
                            break;
                        }
                    case RequestType.BattleCanceled:
                        {
                            ChangeUserControler.CancelBattle();
                            break;
                        }
                    case RequestType.BattleConfirm:
                        {
                            ChangeUserControler.ToFillBattlefield();
                            break;
                        }
                    case RequestType.BattleStarted:
                        {
                            ChangeUserControler.ToBattleField(JsonConvert.DeserializeObject<List<ShipsClass.Ship>>(request.Data[0]), request.Data[1] == true.ToString());
                            break;
                        }
                    case RequestType.BattleEnded:
                        {
                            ChangeUserControler.ToEndScreen(request.Data[0], request.Data[1], request.Data[2]);
                            break;
                        }
                    case RequestType.PlayerReady:
                        {
                            ChangeUserControler.PlayerReady();
                            break;
                        }
                    case RequestType.Fire:
                        {
                            Shoot shoot = JsonConvert.DeserializeObject<Shoot>(request.Data[0]);
                            ChangeUserControler.ShowShootData(shoot);
                            break;
                        }
                    default: throw new Exception();
                }
            }
            //}
            //catch (Exception ex)
            //{
            //    ServerConnection.NetStream?.Close();
            //    ServerConnection.client?.Close();
            //    Environment.Exit(0);
            //}
        }
    }

    class SendToServer
    {
        public static void SendRegisterData(string userName, string login, string password, string email, string registerCode = "")
        {
            Request request = new Request()
            {
                Login = login,
                Password = password,
                Data = new List<string>() { userName, email },
                ReqType = RequestType.Register
            };
            if (registerCode != "")
            {
                request.Data.Add(registerCode);
            }

            SendRequestToServer(request);
        }
        public static void SendLoginData(string login, string password)
        {
            Request request = new Request()
            {
                Login = login,
                Password = password,
                ReqType = RequestType.Login
            };

            SendRequestToServer(request);
        }
        public static void SendFriendRequest(string friendLogin)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { friendLogin },
                ReqType = RequestType.AddFriend
            };

            SendRequestToServer(request);
        }
        public static void SendDeletedFriend(string friendLogin)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { friendLogin },
                ReqType = RequestType.RemoveFriend
            };

            SendRequestToServer(request);
        }
        public static void SendRequestToBattle(string friendLogin)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { friendLogin },
                ReqType = RequestType.BattleRequest
            };

            SendRequestToServer(request);
        }
        public static void SendConfirmBattle(string friendLogin)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { friendLogin },
                ReqType = RequestType.BattleConfirm
            };

            SendRequestToServer(request);
        }
        public static void SendBattleCanceled(string friendLogin)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { friendLogin },
                ReqType = RequestType.BattleCanceled
            };

            SendRequestToServer(request);
        }
        public static void SendPlayerReady(string fieldDataJson)
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { fieldDataJson },
                ReqType = RequestType.PlayerReady
            };

            SendRequestToServer(request);
        }
        public static void SendRewardsRequest()
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                ReqType = RequestType.GetRewards
            };

            SendRequestToServer(request);
        }
        public static void SendFriendsRequest()
        {
            Request request = new Request()
            {
                Login = Settings.Login,
                Password = Settings.Password,
                ReqType = RequestType.GetFriends
            };

            SendRequestToServer(request);
        }
        public static void SendFriendsRequest(string friendLogin) {
            Request request = new Request() {
                Login = friendLogin,
                Password = "",
                ReqType = RequestType.GetFriends
            };

            SendRequestToServer(request);
        }
        public static void SendShoot(string shootData) {
            Request request = new Request() {
                Login = Settings.Login,
                Password = Settings.Password,
                Data = new List<string>() { shootData },
                ReqType = RequestType.Fire
            };

            SendRequestToServer(request);
        }


        public static void SendRequestToServer(Request request)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            ServerConnection.NetStream.Write(data, 0, data.Length);
        }
    }
    #endregion
    #region Request Class
    public class Request
    {
        public string Login = null;
        public string Password = null;
        public List<string> Data = null;
        public RequestType ReqType;
    }

    public enum RequestType
    {
        Register, Login, GetRewards, BattleRequest, BattleConfirm, BattleCanceled, BattleStarted, BattleEnded, Fire, Exception, PlayerReady, AddFriend, RemoveFriend, GetFriends
    }
    #endregion
}