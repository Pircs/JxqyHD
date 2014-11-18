﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine.Script
{
    public class ScriptParser
    {
        private List<Code> _codes;
        private int _currentIndex;
        public string FilePath { private set; get; }
        public bool IsOk { private set; get; }
        public bool IsEnd { private set; get; }
        public ScriptParser() { }

        public ScriptParser(string filePath)
        {
            ReadFile(filePath);
        }

        private static readonly Regex _regGoto = new Regex(@"^@([a-zA-Z]+):");
        private static readonly Regex _regComment = new Regex(@"^//.*");
        private static readonly Regex _regFunction = new Regex(@"^([a-zA-Z]+)(.*);");
        private static readonly Regex _regParameter = new Regex(@"^\((.+)\)(.*)");
        private static readonly Regex _regResult = new Regex(@"^@[a-zA-Z]+");
        private void ParserLine(string line)
        {
            var code = new Code();
            line = line.Trim();
            if (line.Length < 2) return;

            if (_regGoto.IsMatch(line))
            {
                var match = _regGoto.Match(line);
                code.IsGoto = true;
                code.Name = match.Value;
            }
            else if (_regComment.IsMatch(line)) 
            {
                return;
            }
            else if (_regFunction.IsMatch(line))
            {
                var matchFunction = _regFunction.Match(line);
                code.Name = matchFunction.Groups[1].Value;
                var matchParmeter = _regParameter.Match(matchFunction.Groups[2].Value.Trim());
                if (matchParmeter.Success)
                {
                    code.Parameters = ParserParameter(matchParmeter.Groups[1].Value);
                }
                var matchResult = _regResult.Match(matchParmeter.Success
                    ? matchParmeter.Groups[2].Value.Trim()
                    : matchFunction.Groups[2].Value.Trim());
                if (matchResult.Success)
                {
                    code.Result = matchResult.Value;
                }
            }

            _codes.Add(code);
        }

        private List<string> ParserParameter(string str)
        {
            str = str.Trim();
            if (str.Length == 0) return null;
            var parameters = new List<string>();
            var temp = new StringBuilder();
            for (var i = 0; i < str.Length;i++)
            {
                if (str[i] == '"')
                {
                    temp.Append(str[i]);
                    while (str[++i] != '"')
                    {
                        temp.Append(str[i]);
                    }
                    temp.Append(str[i]);
                    parameters.Add(temp.ToString());
                    temp.Clear();
                }
                else if(!char.IsWhiteSpace(str[i]))
                {
                    if (str[i] == ',')
                    {
                        if (temp.Length != 0)
                        {
                            parameters.Add(temp.ToString());
                            temp.Clear();
                        }
                    }
                    else
                    {
                        temp.Append(str[i]);
                    }
                }
            }
            if (temp.Length != 0)
            {
                parameters.Add(temp.ToString());
                temp.Clear();
            }
            return parameters;
        }

        public bool ReadFile(string filePath)
        {
            IsOk = false;
            FilePath = filePath;
            try
            {
                IsOk = ReadFromLines(File.ReadAllLines(filePath, Globals.SimpleChinaeseEncoding));
            }
            catch (Exception exception)
            {
                Log.LogFileLoadError("Script", filePath, exception);
                return false;
            }
            return IsOk;
        }

        public bool ReadFromLines(string[] lines)
        {
            _codes = new List<Code>();
            foreach (var line in lines)
            {
                ParserLine(line);
            }
            return true;
        }

        public void Run()
        {
            IsEnd = false;
            _currentIndex = 0;
            Continue();
        }

        public bool Continue()
        {
            if (IsEnd) return false;
            return true;
        }

        public class Code
        {
            public string Name;
            public List<string> Parameters;
            public string Result;
            public bool IsGoto;
        }

        private enum State
        {
            Normal,
            Comment,
            Function,
            Parmeter,
            Goto,
            Result
        }
    }
}