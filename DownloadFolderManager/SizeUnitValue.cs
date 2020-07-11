using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DownloadFolderManager
{
    public class SizeUnitValue
    {
        public long Length { get; private set; }

        public LogicOperator LogicalOperator { get; private set; }
        public static SizeUnitValue EmptySize => new SizeUnitValue(0, LogicOperator.BiggerOrEquals);

        public SizeUnitValue(long length, LogicOperator logicOperator)
        {
            Length = length;
            LogicalOperator = logicOperator;
        }


        public enum LogicOperator
        {
            BiggerThan,
            BiggerOrEquals,
            SmallerThan,
            SmallerOrEquals,
            Equals
        }
        public static SizeUnitValue ParseSize(string sizeValue)
        {
            if (sizeValue == null) throw new ArgumentNullException("size value is null");
            if (!Regex.IsMatch(sizeValue, @"^(>=|<=|=|>|<)?\s*\d+\s*(KB|MB|GB|TB|B)")) throw new ArgumentException("size value is in invalid format. You need operator (>=,<=,>,<,=) size (decimal value) and unit (B,KB, MB,GB,TB)");

            // we need logic operator on the start
            var operatorMatch = Regex.Match(sizeValue, "(>=|<=|=|>|<)");
            var logicOperator = LogicOperator.Equals;
            if (operatorMatch.Success)
            {
                logicOperator = GetOperatorMatch(operatorMatch.Value);
            }

            var unitMatch = Regex.Match(sizeValue, "(KB|MB|GB|TB|B)");

            long length = long.Parse(Regex.Match(sizeValue, @"\d+").Value);
            long factor = 1;

            if (sizeConvert.TryGetValue(unitMatch.Value.GetHashCode(), out factor))
            {
                length = length * factor;
            }

            return new SizeUnitValue(length, logicOperator);
        }

    


        private static Dictionary<int, long> sizeConvert = new Dictionary<int, long>()
        {
            { "B".GetHashCode(),1},
            { "KB".GetHashCode(),1024},
            { "MB".GetHashCode(),1024*1024},
            { "GB".GetHashCode(),(long)1024*1024*1024},
            { "TB".GetHashCode(),(long)1024*1024*1024*1024},
        };

        public bool Compare(long bit) => LogicalOperator switch
        {
            LogicOperator.Equals => bit == Length,
            LogicOperator.BiggerOrEquals => Length <= bit,
            LogicOperator.BiggerThan => Length < bit,
            LogicOperator.SmallerOrEquals => Length >= bit,
            LogicOperator.SmallerThan => Length > bit,
        };


        static LogicOperator GetOperatorMatch(string value) => value switch
        {
            ">=" => LogicOperator.BiggerThan,
            ">" => LogicOperator.BiggerThan,
            "<" => LogicOperator.SmallerThan,
            "<=" => LogicOperator.SmallerOrEquals,
            "=" => LogicOperator.Equals,
            _ => LogicOperator.Equals,
        };
    }
}
