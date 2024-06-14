﻿using CsvHelper;
using Proteomics.PSM;

namespace Analyzer.Util
{
    public static class Extensions
    {
        /// <summary>
        /// Determine if a PSM is a decoy
        /// </summary>
        /// <param name="psm"></param>
        /// <returns></returns>
        public static bool IsDecoy(this PsmFromTsv psm) => psm.DecoyContamTarget == "D";

        public static Dictionary<int, List<PsmFromTsv>> ToChimeraGroupedDictionary(this IEnumerable<PsmFromTsv> psms)
        {
            return psms.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                .GroupBy(m => m.Count())
                .ToDictionary(p => p.Key, p => p.SelectMany(m => m).ToList());
        }


        public static bool ValidateMyColumn(this IReaderRow row)
        {
            // if I remove the HasHeaderRecord check here and set the CsvConfig HasHeaderRecord = false
            // the code all works I would have originally expected, e.g. header row gets ignored and all othe
            // rows are included.
            if (row.Configuration.HasHeaderRecord && row.Parser.Row == 1)
            {
                return true;
            }

            // Do other checks, for example:

            if (int.TryParse(row[0], out var _))
            {
                return true;
            }

            // Logging to objectForLogRef
            return false;
        }



        /// <summary>
        /// Calculate the rolling average of a list of doubles
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        public static List<double> MovingAverage(this IEnumerable<double> numbers, int windowSize)
        {
            var result = new List<double>();

            for (int i = 0; i < numbers.Count() - windowSize + 1; i++)
            {
                var window = numbers.Skip(i).Take(windowSize);
                result.Add(window.Average());
            }

            return result;
        }

        /// <summary>
        /// Calculates a moving average and skips zeros, then replaces teh zero value with the moving average of teh window around it
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        public static List<double> MovingAverageZeroFill(this IEnumerable<double> numbers, int windowSize)
        {
            var result = new List<double>();

            var enumerable = numbers as double[] ?? numbers.ToArray();
            for (int i = 0; i < enumerable.Count() - windowSize + 1; i++)
            {
                var window = enumerable.Skip(i).Take(windowSize).ToList();
                if (window.Contains(0))
                {
                    var average = window.Where(p => p != 0).Average();
                    result.Add(average);
                }
                else
                {
                    result.Add(window.Average());
                }
            }

            return result;
        }

       

    }
}
