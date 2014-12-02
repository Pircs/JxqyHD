﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Engine.Gui;
using Engine.ListManager;
using Engine.Weather;
using IniParser;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Engine.Script
{
    public static class ScriptExecuter
    {
        private static readonly Dictionary<string, int> Variables = new Dictionary<string, int>();
        private static float _fadeTransparence;
        private static int _talkStartIndex;
        private static int _talkEndIndex;
        private static int _talkCurrentIndex;
        private static float _sleepingMilliseconds;
        private static Color _videoDrawColor;
        private static Video _video;
        private static VideoPlayer _videoPlayer;

        public static bool IsInFadeOut;
        public static bool IsInFadeIn;
        public static bool IsInTalk;
        public static bool IsInSleep;

        public static float FadeTransparence
        {
            get { return _fadeTransparence; }
            private set
            {
                if (value < 0) value = 0;
                if (value > 1) value = 1;
                _fadeTransparence = value;
            }
        }

        public static bool IsInPlayingMovie
        {
            get
            {
                return (_videoPlayer != null &&
                        _videoPlayer.State != MediaState.Stopped);
            }
        }

        private static void GetTargetAndScript(string nameWithQuotes,
            string scriptFileNameWithQuotes,
            object belongObject,
            out Character target,
            out ScriptParser script)
        {
            GetTarget(nameWithQuotes, belongObject, out target);
            var scriptFileName = Utils.RemoveStringQuotes(scriptFileNameWithQuotes);
            script = null;
            if (!string.IsNullOrEmpty(scriptFileName))
                script = new ScriptParser(Utils.GetScriptFilePath(scriptFileName), target);
        }

        private static Character GetPlayerOrNpc(string name)
        {
            if (Globals.ThePlayer != null &&
                Globals.ThePlayer.Name == name)
            {
                return Globals.ThePlayer;
            }
            return NpcManager.GetNpc(name);
        }

        private static void GetTarget(string nameWithQuotes,
            object belongObject,
            out Character target)
        {
            var name = Utils.RemoveStringQuotes(nameWithQuotes);
            target = belongObject as Character;
            if (!string.IsNullOrEmpty(name))
            {
                target = GetPlayerOrNpc(name);
            }
        }

        private static void GetNextTalkTextDeatil(out TalkTextDetail detail)
        {
            detail = null;
            for (; _talkCurrentIndex <= _talkEndIndex; _talkCurrentIndex++)
            {
                detail = TalkTextList.GetTextDetail(_talkCurrentIndex);
                if (detail != null)
                {
                    _talkCurrentIndex++; // Finded, move to next index
                    break;
                }
            }
        }

        private static bool IsPlayerNull()
        {
            return Globals.ThePlayer == null;
        }

        /// <summary>
        /// Used for type: name,x, y or x,y
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="belongObject"></param>
        /// <param name="target"></param>
        /// <param name="value"></param>
        private static void GetTargetAndValue3(List<string> parameters,
            object belongObject,
            out Character target,
            out Vector2 value)
        {
            target = belongObject as Character;
            value = Vector2.Zero;
            if (parameters.Count == 3)
            {
                target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
                value = new Vector2(
                    int.Parse(parameters[1]),
                    int.Parse(parameters[2]));
            }
            else if (parameters.Count == 2)
            {
                value = new Vector2(
                    int.Parse(parameters[0]),
                    int.Parse(parameters[1]));
            }
        }

        /// <summary>
        /// Used for type: name,x or x
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="belongObject"></param>
        /// <param name="target"></param>
        /// <param name="value"></param>
        private static void GetTargetAndValue2(List<string> parameters,
            object belongObject,
            out Character target,
            out int value)
        {
            target = belongObject as Character;
            value = 0;
            if (parameters.Count == 2)
            {
                target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
                value = int.Parse(parameters[1]);
            }
            else if (parameters.Count == 1)
            {
                value = int.Parse(parameters[0]);
            }
        }

        /// <summary>
        /// Used for type: name,x, y or x,y
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="belongObject"></param>
        /// <param name="target"></param>
        /// <param name="value"></param>
        private static void GetTargetAndValue3(List<string> parameters,
            object belongObject,
            out Obj target,
            out Vector2 value)
        {
            target = belongObject as Obj;
            value = Vector2.Zero;
            if (parameters.Count == 3)
            {
                target = ObjManager.GetObj(Utils.RemoveStringQuotes(parameters[0]));
                value = new Vector2(
                    int.Parse(parameters[1]),
                    int.Parse(parameters[2]));
            }
            else if (parameters.Count == 2)
            {
                value = new Vector2(
                    int.Parse(parameters[0]),
                    int.Parse(parameters[1]));
            }
        }

        private static bool IsCharacterMoveEndAndStanding(Character character)
        {
            if (character != null &&
                !character.IsStanding())
            {
                return false;
            }
            Globals.IsInputDisabled = false;
            return true;
        }

        private static bool IsCharacterGotoDirEnd(Character character)
        {
            if (character != null &&
                character.IsInStepMove)
            {
                return false;
            }
            Globals.IsInputDisabled = false;
            return true;
        }

        public static void Update(GameTime gameTime)
        {
            if (IsInFadeOut && FadeTransparence < 1f)
            {
                FadeTransparence += 0.02f;
            }
            else if (IsInFadeIn && FadeTransparence > 0f)
            {
                FadeTransparence -= 0.02f;
                if (FadeTransparence <= 0f) IsInFadeIn = false;
            }

            if (IsInTalk)
            {
                if (GuiManager.IsDialogEnd())
                {
                    TalkTextDetail detail;
                    GetNextTalkTextDeatil(out detail);
                    if (detail != null)
                    {
                        GuiManager.ShowDialog(detail.Text, detail.PortraitIndex);
                    }
                    else
                    {
                        IsInTalk = false;
                    }
                }
            }

            if (IsInSleep)
            {
                _sleepingMilliseconds -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_sleepingMilliseconds <= 0)
                {
                    Globals.IsInputDisabled = false;
                }
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {

        }

        public static void Say(List<string> parameters)
        {
            switch (parameters.Count)
            {
                case 1:
                    GuiManager.ShowDialog(Utils.RemoveStringQuotes(
                        parameters[0]));
                    break;
                case 2:
                    GuiManager.ShowDialog(Utils.RemoveStringQuotes(
                        parameters[0]),
                        int.Parse(parameters[1]));
                    break;
            }
        }

        private static readonly Regex IfParameterPatten = new Regex(@"(\$[a-zA-Z0-9]+) *([><=]+) *(.+)");
        public static bool If(List<string> parameters)
        {
            var parmeter = parameters[0];
            var match = IfParameterPatten.Match(parmeter);
            if (match.Success)
            {
                var groups = match.Groups;
                var variable = groups[1].Value;
                var compare = groups[2].Value;
                var value = int.Parse(groups[3].Value);
                if (Variables.ContainsKey(variable))
                {
                    switch (compare)
                    {
                        case "==":
                            return Variables[variable] == value;
                        case ">>":
                            return Variables[variable] > value;
                        case ">=":
                            return Variables[variable] >= value;
                        case "<<":
                            return Variables[variable] < value;
                        case "<=":
                            return Variables[variable] <= value;
                        case "<>":
                            return Variables[variable] != value;
                    }

                }
            }
            return false;
        }

        public static void Add(List<string> parameters)
        {
            var variable = parameters[0];
            var value = int.Parse(parameters[1]);
            if (!Variables.ContainsKey(variable))
                Variables[variable] = 0;
            Variables[variable] += value;
        }

        public static void Assign(List<string> parameters)
        {
            var variable = parameters[0];
            var value = int.Parse(parameters[1]);
            Variables[variable] = value;
        }

        public static void FadeOut()
        {
            IsInFadeOut = true;
            IsInFadeIn = false;
            FadeTransparence = 0f;
        }

        public static bool IsFadeOutEnd()
        {
            return FadeTransparence >= 1f;
        }

        public static void FadeIn()
        {
            IsInFadeOut = false;
            IsInFadeIn = true;
            FadeTransparence = 1f;
        }

        public static bool IsFadeInEnd()
        {
            return !IsInFadeIn;
        }

        public static void DrawFade(SpriteBatch spriteBatch)
        {
            var fadeTextrue = TextureGenerator.GetColorTexture(
                    Color.Black * ScriptExecuter.FadeTransparence,
                    1,
                    1);
            spriteBatch.Draw(fadeTextrue,
                new Rectangle(0, 0, Globals.WindowWidth, Globals.WindowHeight),
                Color.White);
        }

        public static void DelNpc(List<string> parameters)
        {
            NpcManager.DeleteNpc(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void ClearBody()
        {
            ObjManager.ClearBody();
        }

        public static void StopMusic()
        {
            BackgroundMusic.Stop();
        }

        public static void PlayMusic(List<string> parameters)
        {
            BackgroundMusic.Play(Utils.RemoveStringQuotes(parameters[0]));
        }


        public static void PlaySound(List<string> parameters, object belongObject)
        {
            var fileName = Utils.RemoveStringQuotes(parameters[0]);
            var sound = Utils.GetSoundEffect(fileName);
            var soundPosition = Globals.ListenerPosition;
            var sprit = belongObject as Sprite;
            if (sprit != null) soundPosition = sprit.PositionInWorld;

            SoundManager.Play3DSoundOnece(sound, soundPosition - Globals.ListenerPosition);
        }

        public static void OpenBox(List<string> parameters, object belongObject)
        {
            if (parameters == null)
            {
                var obj = belongObject as Obj;
                if (obj != null)
                {
                    obj.OpenBox();
                }
            }
            else
            {
                var obj = ObjManager.GetObj(Utils.RemoveStringQuotes(parameters[0]));
                if (obj != null)
                {
                    obj.OpenBox();
                }
            }
        }

        public static void SetObjScript(List<string> parameters, object belongObject)
        {
            var name = Utils.RemoveStringQuotes(parameters[0]);
            var scriptFileName = Utils.RemoveStringQuotes(parameters[1]);
            var target = belongObject as Obj;
            ScriptParser script = null;
            if (!string.IsNullOrEmpty(name))
                target = ObjManager.GetObj(name);
            if (!string.IsNullOrEmpty(scriptFileName))
                script = new ScriptParser(Utils.GetScriptFilePath(scriptFileName), target);
            if (target != null)
            {
                target.ScriptFile = script;
            }
        }

        public static void SetNpcScript(List<string> parameters, object belongObject)
        {
            Character target;
            ScriptParser script;
            GetTargetAndScript(parameters[0],
                parameters[1],
                belongObject,
                out target,
                out script);
            if (target != null)
            {
                target.ScriptFile = script;
            }
        }

        public static void SetNpcDeathScript(List<string> parameters, object belongObject)
        {
            Character target;
            ScriptParser script;
            GetTargetAndScript(parameters[0],
                parameters[1],
                belongObject,
                out target,
                out script);
            if (target != null)
            {
                target.DeathScript = script;
            }
        }

        public static void SetNpcLevel(List<string> parameters, object belongObject)
        {
            Character target;
            GetTarget(parameters[0],
                belongObject,
                out target);
            var value = int.Parse(parameters[1]);
            if (target != null)
            {
                target.SetLevelTo(value);
            }
        }

        public static void SetLevelFile(List<string> parameters, object belongObject)
        {
            var target = belongObject as Character;
            if (target != null)
            {
                var path = @"ini\level\" + Utils.RemoveStringQuotes(parameters[0]);
                target.LevelIni = Utils.GetLevelLists(path);
            }
        }

        public static void AddRandMoney(List<string> parameters)
        {
            var min = int.Parse(parameters[0]);
            var max = int.Parse(parameters[1]) + 1;
            var money = Globals.TheRandom.Next(min, max);
            Globals.ThePlayer.AddMoney(money);
        }

        public static void AddLife(List<string> parameters)
        {
            var value = int.Parse(parameters[0]);
            Globals.ThePlayer.AddLife(value);
        }

        public static void AddThew(List<string> parameters)
        {
            var value = int.Parse(parameters[0]);
            Globals.ThePlayer.AddThew(value);
        }

        public static void AddMana(List<string> parameters)
        {
            var value = int.Parse(parameters[0]);
            Globals.ThePlayer.AddMana(value);
        }

        public static void AddExp(List<string> parameters)
        {
            Globals.ThePlayer.AddExp(int.Parse(parameters[0]));
        }

        public static void SetPlayerPos(List<string> parameters)
        {
            var x = int.Parse(parameters[0]);
            var y = int.Parse(parameters[1]);
            Globals.ThePlayer.SetPlayerTilePosition(new Vector2(x, y));
        }

        public static void SetPlayerDir(List<string> parameters)
        {
            Globals.ThePlayer.SetDirection(int.Parse(parameters[0]));
        }

        public static void LoadMap(List<string> parameters)
        {
            WeatherManager.StopRain();
            Globals.TheMap.LoadMap(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void LoadNpc(List<string> parameters)
        {
            NpcManager.Load(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void LoadObj(List<string> parameters)
        {
            ObjManager.Load(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void AddGoods(List<string> parameters)
        {
            AddGoods(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void AddGoods(string fileName)
        {
            int index;
            Good good;
            var result = GoodsListManager.AddGoodToList(
                fileName,
                out index,
                out good);
            if (result && good != null)
            {
                GuiManager.ShowMessage("你获得了" + good.Name);
            }
            else
            {
                GuiManager.ShowMessage("错误");
            }
            GuiManager.UpdateGoodsView();
        }

        public static void AddRandGoods(List<string> parameters)
        {
            var fileName = GetRandItem(@"ini\buy\" + Utils.RemoveStringQuotes(parameters[0]));
            if (string.IsNullOrEmpty(fileName)) return;
            AddGoods(fileName);
        }

        public static string GetRandItem(string filePath)
        {
            try
            {
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(filePath);
                var count = int.Parse(data["Header"]["Count"]);
                var rand = Globals.TheRandom.Next(1, count + 1);
                return data[rand.ToString()]["IniFile"];
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static void AddMagic(List<string> parameters)
        {
            int index;
            Magic magic;
            var result = MagicListManager.AddMagicToList(
                Utils.RemoveStringQuotes(parameters[0]),
                out index,
                out magic);
            if (result)
            {
                GuiManager.ShowMessage("你学会了" + magic.Name);
                GuiManager.UpdateMagicView();
            }
            else
            {
                if (magic != null)
                {
                    GuiManager.ShowMessage("你已经学会了" + magic.Name);
                }
                else
                {
                    GuiManager.ShowMessage("错误");
                }
            }
        }

        public static void AddMoney(List<string> parameters)
        {
            Globals.ThePlayer.AddMoney(int.Parse(parameters[0]));
        }

        public static void AddNpc(List<string> parameters)
        {
            NpcManager.AddNpc(Utils.RemoveStringQuotes(parameters[0]),
                int.Parse(parameters[1]),
                int.Parse(parameters[2]),
                int.Parse(parameters[3]));
        }

        public static void AddObj(List<string> parameters)
        {
            ObjManager.AddObj(Utils.RemoveStringQuotes(parameters[0]),
                int.Parse(parameters[1]),
                int.Parse(parameters[2]),
                int.Parse(parameters[3]));
        }

        public static void Talk(List<string> parameters)
        {
            IsInTalk = true;
            _talkStartIndex = int.Parse(parameters[0]);
            _talkEndIndex = int.Parse(parameters[1]);
            _talkCurrentIndex = _talkStartIndex;
            TalkTextDetail detail;
            GetNextTalkTextDeatil(out detail);
            if (detail != null)
            {
                GuiManager.ShowDialog(detail.Text, detail.PortraitIndex);
            }
            else
            {
                IsInTalk = false;
            }
        }

        public static void Memo(List<string> parameters)
        {
            GuiManager.AddMemo(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void AddToMemo(List<string> parameters)
        {
            var detail = TalkTextList.GetTextDetail(int.Parse(parameters[0]));
            if (detail == null) return;
            GuiManager.AddMemo(detail.Text);
        }

        public static void DelGoods(List<string> parameters, object belongObject)
        {
            if (parameters == null)
            {
                var good = belongObject as Good;
                if (good != null)
                {
                    GuiManager.DeleteGood(good.FileName);
                }
            }
            else
            {
                GuiManager.DeleteGood(Utils.RemoveStringQuotes(parameters[0]));
            }
        }

        public static void DelCurObj(object belongObject)
        {
            var obj = belongObject as Obj;
            if (obj == null) return;
            obj.IsRemoved = true;
        }

        public static void DelObj(List<string> parameters, object belongObject)
        {
            if (parameters == null || parameters[0] == "\"\"")
            {
                DelCurObj(belongObject);
            }
            else
            {
                ObjManager.DeleteObj(Utils.RemoveStringQuotes(parameters[0]));
            }
        }

        public static void FreeMap()
        {
            if (Globals.TheMap != null)
            {
                Globals.TheMap.Free();
            }
        }

        public static void SetTrap(List<string> parameters)
        {
            Globals.TheMap.SetMapTrap(int.Parse(parameters[1]),
                Utils.RemoveStringQuotes(parameters[2]),
                Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void SetMapTrap(List<string> parameters)
        {
            Globals.TheMap.SetMapTrap(int.Parse(parameters[0]),
                Utils.RemoveStringQuotes(parameters[1]));
        }

        public static void FullLife()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.FullLife();
            }
        }

        public static void FullMana()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.FullMana();
            }
        }

        public static void FullThew()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.FullThew();
            }
        }

        public static void ShowNpc(List<string> parameters)
        {
            var name = Utils.RemoveStringQuotes(parameters[0]);
            var show = (int.Parse(parameters[1]) != 0);
            NpcManager.ShowNpc(name, show);
        }

        public static void Sleep(List<string> parameters)
        {
            _sleepingMilliseconds = int.Parse(parameters[0]);
            Globals.IsInputDisabled = true;
        }

        public static void ShowMessage(List<string> parameters)
        {
            var detail = TalkTextList.GetTextDetail(int.Parse(parameters[0]));
            if (detail != null)
            {
                GuiManager.ShowMessage(detail.Text);
            }
        }

        public static void SetMagicLevel(List<string> parameters)
        {
            var fileName = Utils.RemoveStringQuotes(parameters[0]);
            var level = int.Parse(parameters[1]);
            MagicListManager.SetMagicLevel(fileName, level);
            GuiManager.XiuLianInterface.UpdateItem();
        }

        public static void ShowSnow(List<string> parameters)
        {
            var isShow = (int.Parse(parameters[0]) != 0);
            WeatherManager.ShowSnow(isShow);
        }

        public static void BeginRain(List<string> parameters)
        {
            WeatherManager.BeginRain(Utils.RemoveStringQuotes(parameters[0]));
        }

        public static void EndRain()
        {
            WeatherManager.StopRain();
        }

        public static void ChangeMapColor(List<string> parameters)
        {
            var color = new Color(int.Parse(parameters[0]),
                int.Parse(parameters[1]),
                int.Parse(parameters[2]));
            Map.DrawColor = color;
        }

        public static void ChangeAsfColor(List<string> parameters)
        {
            var color = new Color(int.Parse(parameters[0]),
                int.Parse(parameters[1]),
                int.Parse(parameters[2]));
            Sprite.DrawColor = color;
        }

        public static void Choose(List<string> parameters)
        {
            GuiManager.Selection(Utils.RemoveStringQuotes(parameters[0]),
                Utils.RemoveStringQuotes(parameters[1]),
                Utils.RemoveStringQuotes(parameters[2]));
        }

        public static void Select(List<string> parameters)
        {
            GuiManager.Selection(TalkTextList.GetTextDetail(int.Parse(parameters[0])).Text,
                TalkTextList.GetTextDetail(int.Parse(parameters[1])).Text,
                TalkTextList.GetTextDetail(int.Parse(parameters[2])).Text);
        }

        public static bool IsChooseEnd(List<string> parameters)
        {
            if (GuiManager.IsSelectionEnd())
            {
                Variables[parameters[3]] = GuiManager.GetSelection();
                return true;
            }
            return false;
        }

        public static void RunScript(List<string> parameters, object belongObject)
        {
            ScriptManager.RunScript(new ScriptParser(
                Utils.GetScriptFilePath(Utils.RemoveStringQuotes(parameters[0])),
                belongObject));
        }

        public static void PlayMovie(string fileName, Color drawColor)
        {
            _video = Utils.GetVideo(fileName);
            if (_video == null) return;
            _videoPlayer = new VideoPlayer();
            if (_videoPlayer == null) return;
            _videoDrawColor = drawColor;

            _videoPlayer.Play(_video);
        }

        public static void PlayMovie(string fileName)
        {
            PlayMovie(fileName, Color.White);
        }

        public static void PlayMovie(List<string> parameters)
        {
            var fileName = Utils.RemoveStringQuotes(parameters[0]);
            var color = Color.White;
            if (parameters.Count == 4)
            {
                color = new Color(
                    int.Parse(parameters[1]),
                    int.Parse(parameters[2]),
                    int.Parse(parameters[3]));
            }
            PlayMovie(fileName, color);
        }

        public static void StopMovie()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        public static void DrawVideo(SpriteBatch spriteBatch)
        {
            if (_videoPlayer != null && _videoPlayer.State != MediaState.Stopped)
            {
                var texture = _videoPlayer.GetTexture();
                if (texture != null)
                {
                    spriteBatch.Draw(texture,
                        new Rectangle(0, 0, Globals.WindowWidth, Globals.WindowHeight),
                        _videoDrawColor);
                }
            }
        }

        public static bool IsMovePlayEnd()
        {
            if (_videoPlayer != null)
            {
                return _videoPlayer.State == MediaState.Stopped;
            }
            return true;
        }

        public static void SaveMapTrap()
        {
            Globals.TheMap.SaveTrap(@"save\game\Traps.ini");
        }

        public static void SaveNpc(List<string> parameters)
        {
            string fileName = null;
            if (parameters.Count == 1)
            {
                fileName = Utils.RemoveStringQuotes(parameters[0]);
            }
            NpcManager.Save(fileName);
        }

        public static void SaveObj(List<string> parameters)
        {
            string fileName = null;
            if (parameters.Count == 1)
            {
                fileName = Utils.RemoveStringQuotes(parameters[0]);
            }
            ObjManager.Save(fileName);
        }

        public static void GetRandNum(List<string> parameters)
        {
            Variables[parameters[0]] = Globals.TheRandom.Next(
                int.Parse(parameters[1]),
                int.Parse(parameters[2]) + 1);
        }

        public static void SetNpcDir(List<string> parameters, object belongObject)
        {
            Character target;
            int value;
            GetTargetAndValue2(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetDirection(value);
            }
        }

        public static void SetNpcKind(List<string> parameters, object belongObject)
        {
            Character target;
            int value;
            GetTargetAndValue2(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetKind(value);
            }
        }

        public static void SetNpcMagicFile(List<string> parameters, object belongObject)
        {
            Character target = null;
            var fileName = "";
            if (parameters.Count == 1)
            {
                target = belongObject as Character;
                fileName = Utils.RemoveStringQuotes(parameters[0]);
            }
            else if (parameters.Count == 2)
            {
                target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
                fileName = Utils.RemoveStringQuotes(parameters[1]);
            }
            if (target != null)
            {
                target.SetMagicFile(fileName);
            }
        }

        public static void SetNpcPos(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 value;
            GetTargetAndValue3(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetTilePosition(value);
            }
        }

        public static void SetNpcRelation(List<string> parameters, object belongObject)
        {
            Character target;
            int value;
            GetTargetAndValue2(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetRelation(value);
            }
        }

        public static void SetNpcRes(List<string> parameters, object belongObject)
        {
            Character target = null;
            var fileName = "";
            if (parameters.Count == 1)
            {
                target = belongObject as Character;
                fileName = Utils.RemoveStringQuotes(parameters[0]);
            }
            else if (parameters.Count == 2)
            {
                target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
                fileName = Utils.RemoveStringQuotes(parameters[1]);
            }
            if (target != null)
            {
                target.SetRes(fileName);
            }
        }

        public static void SetNpcAction(List<string> parameters)
        {
            var target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
            var action = (CharacterState)int.Parse(parameters[1]);
            var destination = Vector2.Zero;
            if (parameters.Count == 4)
            {
                destination = new Vector2(int.Parse(parameters[2]),
                    int.Parse(parameters[3]));
            }
            if (target == null) return;
            switch (action)
            {
                case CharacterState.Stand:
                case CharacterState.Stand1:
                    target.NextStepStaning();
                    break;
                case CharacterState.Walk:
                    target.WalkTo(destination);
                    break;
                case CharacterState.Run:
                    target.RunTo(destination);
                    break;
                case CharacterState.Jump:
                    target.JumpTo(destination);
                    break;
                case CharacterState.Attack:
                case CharacterState.Attack1:
                case CharacterState.Attack2:
                    target.PerformeAttack(destination);
                    break;
                case CharacterState.Magic:
                    target.UseMagic(target.FlyIni, destination);
                    break;
                case CharacterState.Sit:
                    target.Sitdown();
                    break;
                case CharacterState.Hurt:
                    target.Hurting();
                    break;
                case CharacterState.Death:
                    target.Death();
                    break;
                case CharacterState.FightStand:
                    target.NextStepStaning();
                    target.ToFightingState();
                    break;
                case CharacterState.FightWalk:
                    target.WalkTo(destination);
                    target.ToFightingState();
                    break;
                case CharacterState.FightRun:
                    target.RunTo(destination);
                    target.ToFightingState();
                    break;
                case CharacterState.FightJump:
                    target.JumpTo(destination);
                    target.ToFightingState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void SetNpcActionFile(List<string> parameters)
        {
            var target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
            var state = (CharacterState)int.Parse(parameters[1]);
            var fileName = Utils.RemoveStringQuotes(parameters[2]);
            if (target != null)
            {
                target.SetNpcActionFile(state, fileName);
            }
        }

        public static void SetNpcActionType(List<string> parameters, object belongObject)
        {
            Character target;
            int value;
            GetTargetAndValue2(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetNpcActionType(value);
            }
        }

        public static void Watch(List<string> parameters)
        {
            var watcher = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[0]));
            var target = GetPlayerOrNpc(Utils.RemoveStringQuotes(parameters[1]));
            if (watcher != null && target != null)
            {
                watcher.SetDirection(target.PositionInWorld - watcher.PositionInWorld);
            }
        }

        public static void SetObjOfs(List<string> parameters, object belongObject)
        {
            Obj target;
            Vector2 value;
            GetTargetAndValue3(parameters, belongObject, out target, out value);
            if (target != null)
            {
                target.SetOffSet(value);
            }
        }

        public static void DisableFight()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.DisableFight();
            }
        }

        public static void EnableFight()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.EnableFight();
            }
        }

        public static void DisableInput()
        {
            Globals.IsInputDisabled = true;
        }

        public static void EnableInput()
        {
            Globals.IsInputDisabled = false;
        }

        public static void DisableJump()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.DisableJump();
            }
        }

        public static void EnableJump()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.EnableJump();
            }
        }

        public static void DisableNpcAI()
        {
            Npc.DisableAI();
        }

        public static void EnableNpcAI()
        {
            Npc.EnableAI();
        }

        public static void DisableRun()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.DisableRun();
            }
        }

        public static void EnableRun()
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.EnableRun();
            }
        }

        public static void SetPlayerState(List<string> parameters)
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.SetFightState(int.Parse(parameters[0]) != 0);
            }
        }

        public static void OpenWaterEffect()
        {
            Globals.IsWaterEffectEnabled = true;
        }

        public static void CloseWaterEffect()
        {
            Globals.IsWaterEffectEnabled = false;
        }

        public static void PlayerGoto(List<string> parameters)
        {
            var tilePosition = new Vector2(
                int.Parse(parameters[0]),
                int.Parse(parameters[1]));
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.WalkTo(tilePosition);
                Globals.IsInputDisabled = true;
            }
        }

        public static bool IsPlayerGotoEnd()
        {
            return IsCharacterMoveEndAndStanding(Globals.ThePlayer);
        }

        public static void PlayerGotoDir(List<string> parameters)
        {
            if (IsPlayerNull()) return;
            Globals.ThePlayer.WalkToDirection(int.Parse(parameters[0]),
                int.Parse(parameters[1]));
            Globals.IsInputDisabled = true;
        }

        public static bool IsPlayerGotoDirEnd()
        {
            return IsCharacterGotoDirEnd(Globals.ThePlayer);
        }

        public static void PlayerGotoEx(List<string> parameters)
        {
            if (Globals.ThePlayer != null)
            {
                Globals.ThePlayer.WalkTo(new Vector2(
                    int.Parse(parameters[0]),
                    int.Parse(parameters[1])));
            }
        }

        public static void PlayerJumpTo(List<string> parameters)
        {
            if (IsPlayerNull()) return;
            Globals.IsInputDisabled = true;
            Globals.ThePlayer.JumpTo(new Vector2(
                int.Parse(parameters[0]),
                int.Parse(parameters[1])));
        }

        public static bool IsPlayerJumpToEnd()
        {
            return IsCharacterMoveEndAndStanding(Globals.ThePlayer);
        }

        public static void PlayerRunTo(List<string> parameters)
        {
            if (IsPlayerNull()) return;
            Globals.IsInputDisabled = true;
            Globals.ThePlayer.RunTo(new Vector2(
                int.Parse(parameters[0]),
                int.Parse(parameters[1])));
        }

        public static bool IsPlayerRunToEnd()
        {
            return IsCharacterMoveEndAndStanding(Globals.ThePlayer);
        }

        public static void PlayerRunToEx(List<string> parameters)
        {
            if (IsPlayerNull()) return;
            Globals.ThePlayer.RunTo(new Vector2(
                int.Parse(parameters[0]),
                int.Parse(parameters[1])));
        }

        public static void NpcGoto(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 destination;
            GetTargetAndValue3(parameters, belongObject, out target, out destination);
            if (target != null)
            {
                Globals.IsInputDisabled = true;
                target.WalkTo(destination);
            }
        }

        public static bool IsNpcGotoEnd(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 destination;
            GetTargetAndValue3(parameters, belongObject, out target, out destination);
            if (target != null && !target.IsStanding())
            {
                return false;
            }
            Globals.IsInputDisabled = false;
            return true;
        }

        public static void NpcGotoDir(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 value;
            GetTargetAndValue3(parameters, belongObject, out target, out value);
            if (target != null)
            {
                Globals.IsInputDisabled = true;
                target.WalkToDirection((int)value.X,
                    (int)value.Y);
            }
        }

        public static bool IsNpcGotoDirEnd(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 value;
            GetTargetAndValue3(parameters, belongObject, out target, out value);
            return IsCharacterGotoDirEnd(target);
        }

        public static void NpcGotoEx(List<string> parameters, object belongObject)
        {
            Character target;
            Vector2 position;
            GetTargetAndValue3(parameters, belongObject, out target, out position);
            if (target != null)
            {
                target.WalkTo(position);
            }
        }

        public static void SetMoneyNum(List<string> parameters)
        {
            if(IsPlayerNull()) return;
            Globals.ThePlayer.SetMoney(int.Parse(parameters[0]));
        }

        public static void GetMoneyNum(List<string> parameters)
        {
            var name = "$MoneyNum";
            if (parameters != null)
            {
                name = parameters[0];
            }
            var value = 0;
            if (!IsPlayerNull())
            {
                value = Globals.ThePlayer.GetMoneyAmount();
            }
            Variables[name] = value;
        }

        public static void SetPlayerScn()
        {
            if (Globals.TheCarmera != null)
            {
                Globals.TheCarmera.PlayerToCenter();
            }
        }

        public static void MoveScreen(List<string> parameters)
        {
            if (Globals.TheCarmera == null) return;
            var direction = Utils.GetDirection8(int.Parse(
                parameters[0]));
            Globals.TheCarmera.MoveTo(direction,
                int.Parse(parameters[1]),
                int.Parse(parameters[2]));
            Globals.IsInputDisabled = true;
        }

        public static bool IsMoveScreenEnd()
        {
            if (Globals.TheCarmera != null &&
                Globals.TheCarmera.IsInMove)
            {
                return false;
            }
            Globals.IsInputDisabled = false;
            return true;
        }

        public static void MoveScreenEx(List<string> parameters)
        {
            if (Globals.TheCarmera == null) return;
            Globals.TheCarmera.MoveTo(new Vector2(
                int.Parse(parameters[0]), 
                int.Parse(parameters[1])),
                int.Parse(parameters[2]));
            Globals.IsInputDisabled = true;
        }

        public static bool IsMoveScreenExEnd()
        {
            if (Globals.TheCarmera != null &&
                Globals.TheCarmera.IsInMoveTo)
            {
                return false;
            }
            Globals.IsInputDisabled = false;
            return true;
        }

        public static void SetMapPos(List<string> parameters)
        {
            if (Globals.TheCarmera == null) return;
            Globals.TheCarmera.CarmeraBeginPositionInWorld =
                Map.ToPixelPosition(int.Parse(parameters[0]),
                    int.Parse(parameters[1]));
        }

        public static void EquipGoods(List<string> parameters)
        {
            var index = int.Parse(parameters[0]);
            var part = (Good.EquipPosition) int.Parse(parameters[1]);
            GuiManager.EquipGoods(index, part);
        }
    }
}