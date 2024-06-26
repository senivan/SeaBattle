﻿using System;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RegistrationNS;
using GameDBContext.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using ShipsClass;
using static System.Net.Mime.MediaTypeNames;

namespace SeaBattleServer
{
    #region Request Class
    public class Request
    {
        public string Login = null;
        public string Password = null;
        public List<string> Data = null;
        public RequestType ReqType;

        public void Execute()
        {
            Request request = new Request() { Data = new List<string>() };
            switch (ReqType)
            {
                case RequestType.Login:
                    {
                        request.ReqType = RequestType.Login;
                        request.Data.Add(
                            ((from users in DataBaseAccess.DbContext.Users
                              where users.Login == Login
                              && users.Password == Password
                              && users.Registration == null
                              select users).FirstOrDefault() != null).ToString());
                        break;
                    }
                case RequestType.Register:
                    {
                        RegEmail regEmail = new RegEmail(DataBaseAccess.DbContext, Data[0]/*NickName*/, Data[1]/*Email*/, Login, Password);
                        if (Data.Count == 2) //2 це нікнейм і пошта, 3 це код
                        {
                            bool isLoginExist = (from u in DataBaseAccess.DbContext.Users
                                                 where u.Login == Login && u.Registration == null
                                                 select u).FirstOrDefault() != null;
                            if (isLoginExist)
                            {
                                request.Data.Add("This login already exists!");
                                break;
                            }
                            regEmail.sendCode();
                            request.Data.Add(true.ToString());
                        }
                        else
                        {
                            request.Data.Add(true.ToString());
                            if (regEmail.checkCode(Data[2]))//code
                            {
                                request.Data.Add(true.ToString());
                            }
                            else
                            {
                                request.Data.Add("Wrong code!");
                            }
                        }
                        break;
                    }
            }

            if ((from u in DataBaseAccess.DbContext.Users
                 where u.Registration == null
                       && u.Login == Login
                       && u.Password == Password
                 select u).FirstOrDefault() == null && ReqType != RequestType.Login && ReqType != RequestType.Register)
            {
                return;
            }

            switch (ReqType)
            {
                case RequestType.GetRewards:
                    {
                        Reward rewards = (from u in DataBaseAccess.DbContext.Users
                                          join r in DataBaseAccess.DbContext.Rewards on u.Reward equals r
                                          where u.Registration == null
                                          && u.Login == Login
                                          select r).FirstOrDefault();
                        request.ReqType = RequestType.GetRewards;
                        if (rewards != null)
                        {
                            request.Data.Add(rewards.BattlesWon.ToString());
                            request.Data.Add(rewards.BattlesPlayed.ToString());
                        }
                        break;
                    }
                case RequestType.AddFriend:
                    {
                        request.ReqType = RequestType.AddFriend;
                        try
                        {
                            FriendsManagement.AddFriend(Login, Data[0]);
                            request.Data.Add("User added to friends!");
                        }
                        catch
                        {
                            request.Data.Add("User not found!");
                        }
                        break;
                    }
                case RequestType.RemoveFriend:
                    {
                        request.ReqType = RequestType.RemoveFriend;
                        try
                        {
                            FriendsManagement.RemoveFriend(Login, Data[0]);
                            request.Data.Add("User removed from friends!");
                        }
                        catch
                        {
                            request.Data.Add("Something went wrong!");
                        }
                        break;
                    }
                case RequestType.GetFriends:
                    {
                        request.ReqType = RequestType.GetFriends;
                        List<string> from = (from f in DataBaseAccess.DbContext.Friends
                                             where f.User1.Login == Login
                                             && f.User1.Registration == null
                                             select f.User2.Login).ToList();
                        List<string> to = (from f in DataBaseAccess.DbContext.Friends
                                           where f.User2.Login == Login
                                           && f.User2.Registration == null
                                           select f.User1.Login).ToList();
                        List<string> friends = new List<string>();
                        foreach (var item in from)
                        {
                            if (to.FindIndex(f => f == item) != -1)
                            {
                                friends.Add(item);
                            }
                        }

                        request.Data.Add(JsonConvert.SerializeObject(friends));
                        request.Data.Add(JsonConvert.SerializeObject(to));
                        break;
                    }
                case RequestType.BattleRequest:
                    {
                        request.ReqType = RequestType.BattleRequest;
                        try
                        {
                            BattleManagement.createBattle(Login, Data[0], "", "");//створюємо бій
                        }
                        catch (Exception ex)
                        {
                            request.Data.Add(ex.Message);//якзо юзер вже в бою
                            break;
                        }
                        User user = (from u in DataBaseAccess.DbContext.Users
                                     where u.Login == Login && u.RegistrationId == null
                                     select u).FirstOrDefault(); //гравець кинувший реквест
                        Request friendNotify = new Request()
                        {
                            Login = user.Login,
                            ReqType = RequestType.BattleRequest,
                            Data = new List<string>() { user.Nickname }
                        };
                        if (!ServerObj.SendToClientByLogin(Data[0], friendNotify))//якщо юзер офлайн
                        {
                            var bId = (from u in DataBaseAccess.DbContext.Users
                                       where u.Login == Login && u.RegistrationId == null
                                       select u.CurrentBattleId).FirstOrDefault();
                            BattleManagement.deleteBattle(bId);
                            request.Data.Add("User is offline");
                        }
                        else//успіх
                        {
                            return;
                        }
                        break;
                    }
                case RequestType.BattleConfirm:
                    {
                        request.ReqType = RequestType.BattleConfirm;
                        if ((from u in DataBaseAccess.DbContext.Users
                             where u.Login == Login
                             && u.RegistrationId == null
                             select u.CurrentBattleId).FirstOrDefault()
                             == (from u in DataBaseAccess.DbContext.Users
                                 where u.Login == Data[0]
                                 && u.RegistrationId == null
                                 select u.CurrentBattleId).FirstOrDefault())
                        {
                            ServerObj.SendToClientByLogin(Data[0], request);
                        }
                        return;
                    }
                case RequestType.BattleCanceled:
                    {
                        request.ReqType = RequestType.BattleCanceled;

                        var bId = (from u in DataBaseAccess.DbContext.Users
                                   where u.Login == Login && u.RegistrationId == null
                                   select u.CurrentBattleId).FirstOrDefault();
                        BattleManagement.deleteBattle(bId);

                        Request friendNotify = new Request()
                        {
                            ReqType = RequestType.BattleCanceled,
                            Login = Login
                        };
                        ServerObj.SendToClientByLogin(Data[0], friendNotify);

                        return;
                    }
                case RequestType.BattleEnded:
                    {
                        BattleManagement.deleteBattle((from u in DataBaseAccess.DbContext.Users
                                                       where u.Login == Login && u.RegistrationId == null
                                                       select u.CurrentBattleId).FirstOrDefault());
                        break;
                    }
                case RequestType.PlayerReady:
                    {
                        request.ReqType = RequestType.PlayerReady;
                        CurrentBattle battle = (from u in DataBaseAccess.DbContext.Users
                                                join b in DataBaseAccess.DbContext.CurrentBattles on u.CurrentBattleId equals b.Id
                                                where u.Login == Login && u.RegistrationId == null
                                                select b).FirstOrDefault();
                        List<User> users = (from u in DataBaseAccess.DbContext.Users
                                            where u.CurrentBattleId == battle.Id
                                            orderby u.Id
                                            select u).ToList();

                        List<Ship> ships = JsonConvert.DeserializeObject<List<Ship>>(Data[0]);
                        foreach (var ship in ships)
                        {

                            foreach (var decks in ship.Decks)
                            {
                                if (decks.Coords.X >= 10 || decks.Coords.Y >= 10)
                                {
                                    return;
                                }
                            }
                            foreach (var placedShip in ships)
                            {
                                foreach (var placedDeck in placedShip.Decks)
                                {
                                    foreach (var decks in ship.Decks)
                                    {
                                        if ((decks.Coords.X >= placedDeck.Coords.X - 1 && decks.Coords.X <= placedDeck.Coords.X + 1
                                        && decks.Coords.Y >= placedDeck.Coords.Y - 1 && decks.Coords.Y <= placedDeck.Coords.Y + 1))
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                          
                        }

                        string friendLogin;
                        if (users[0].Login == Login)
                        {
                            battle.FirstFieldData = Data[0];
                            friendLogin = users[1].Login;
                        }
                        else
                        {
                            battle.SecondFieldData = Data[0];
                            friendLogin = users[0].Login;
                        }
                        DataBaseAccess.DbContext.CurrentBattles.Update(battle);
                        DataBaseAccess.DbContext.SaveChanges();

                        if (battle.FirstFieldData != "" && battle.SecondFieldData != "") // Якщо готові двоє гравців
                        {
                            request.ReqType = RequestType.BattleStarted;
                            request.Data.Add(battle.FirstFieldData);
                            request.Data.Add(battle.Move.ToString());
                            ServerObj.SendToClientByLogin(users[0].Login, request);

                            request.Data.Clear();
                            request.Data.Add(battle.SecondFieldData);
                            request.Data.Add((!battle.Move).ToString());
                            ServerObj.SendToClientByLogin(users[1].Login, request);
                            return;
                        }
                        ServerObj.SendToClientByLogin(friendLogin, request);
                        return;
                    }
                case RequestType.Fire:
                    {
                        request.ReqType = RequestType.Fire;
                        var battle = (from b in DataBaseAccess.DbContext.CurrentBattles
                                      where b.Id == (from u in DataBaseAccess.DbContext.Users
                                                     where u.Login == Login
                                                     select u.CurrentBattleId).FirstOrDefault()
                                      select b).FirstOrDefault(); //находить бій
                        List<User> users = (from u in DataBaseAccess.DbContext.Users
                                            where u.CurrentBattleId == battle.Id
                                            orderby u.Id
                                            select u).ToList();
                        if ((users[0].Login == Login && !battle.Move) || (users[0].Login != Login && battle.Move))
                        {
                            return;
                        }

                        Request request1 = new Request()
                        {
                            ReqType = RequestType.Fire,
                            Data = Data
                        };
                        ServerObj.SendToClientByLogin((from u in users where u.Login != Login select u.Login).FirstOrDefault(), request1);// відправляє противнику постріл

                        string jsonData;
                        if (users[0].Login == Login)//витягує поле
                        {
                            jsonData = battle.SecondFieldData;
                        }
                        else
                        {
                            jsonData = battle.FirstFieldData;
                        }

                        Shoot shoot = JsonConvert.DeserializeObject<Shoot>(Data[0]);
                        List<Ship> shipList = JsonConvert.DeserializeObject<List<Ship>>(jsonData);

                        shoot.Damage(ref shipList);
                        bool isWiner = true;
                        foreach (var item in shipList)
                        {
                            if (!isWiner)
                            {
                                break;
                            }
                            foreach(var deck in item.Decks)
                            {
                                if (deck.IsDamaged == false)
                                {
                                    isWiner = false;
                                    break;
                                }
                            }
                        }
                        if (isWiner)
                        {
                            request.ReqType = RequestType.BattleEnded;
                            request.Data.Add(Login);
                            request.Data.Add(battle.FirstFieldData);
                            request.Data.Add(battle.SecondFieldData);
                            ServerObj.SendToClientByLogin(Login, request);
                            ServerObj.SendToClientByLogin((from u in users where u.Login != Login select u.Login).FirstOrDefault(), request);
                            BattleManagement.deleteBattle(battle.Id);
                            return;
                        }

                        if (shoot.ReturnedValue == -1)
                        {
                            BattleManagement.switchMove(Login);
                        }

                        jsonData = JsonConvert.SerializeObject(shipList);
                        if (users[0].Login == Login)//оновлює поле
                        {
                            BattleManagement.updateField(users[1].Login, jsonData);
                        }
                        else
                        {
                            BattleManagement.updateField(users[0].Login, jsonData);
                        }

                        jsonData = JsonConvert.SerializeObject(shoot);
                        request.Data.Add(jsonData);//відправляє данні про постріл
                        break;
                    }
            }
            ServerObj.SendToClientByLogin(Login, request);
        }
    }


    public enum RequestType
    {
        Register, Login, GetRewards, BattleRequest, BattleConfirm, BattleCanceled, BattleStarted, BattleEnded, Fire, Exception, PlayerReady, AddFriend, RemoveFriend, GetFriends
    }
    #endregion
}