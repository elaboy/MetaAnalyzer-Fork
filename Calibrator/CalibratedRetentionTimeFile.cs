﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.External;
using CsvHelper;
using CsvHelper.Configuration;
using Readers;

namespace Calibrator
{
    public class CalibratedRetentionTimeFile : ResultFile<CalibratedRetentionTimeRecord>, IResultFile
    {
        public override SupportedFileType FileType { get; }
        public override Software Software { get; set; }
        public CalibratedRetentionTimeFile(string filePath) : base(filePath, Software.Unspecified)
        {
        }

        public override void LoadResults()
        {
            using var sr = new StreamReader(FilePath);
            List<CalibratedRetentionTimeRecord> results = new();
            bool foundHeader = false;
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line is null)
                    continue;
                if (foundHeader == false)
                {
                    foundHeader = true;
                    continue;
                }
                var values = line.Split(',');
                if (values.Length < 2)
                    continue;
                var fullSequence = values[0];
                var adjustedRetentionTimes = new List<(string FileName, double AdjustedRetentionTime)>();
                for (int i = 1; i < values.Length; i++)
                {
                    if (string.IsNullOrEmpty(values[i]))
                        continue;
                    adjustedRetentionTimes.Add((values[i], double.Parse(values[i + 1])));
                    i++;
                }

                results.Add(new CalibratedRetentionTimeRecord(fullSequence, adjustedRetentionTimes));
            }

            Results = results;
        }

        public override void WriteResults(string outputPath)
        {
            using var sw = new StreamWriter(File.Create(outputPath));

            var header = "FullSequence,";
            var toAdd = Results.SelectMany(p => p.AdjustedRetentionTimes.Keys)
                .Distinct().ToArray();
            var newHeader = string.Join(',', header + toAdd);
            sw.WriteLine(newHeader);

            foreach (var result in Results)
            {
                string resultText = result.FullSequence + ',';
                for (int i = 0; i < toAdd.Length; i++)
                {
                    if (result.AdjustedRetentionTimes.TryGetValue(toAdd[i], out double val))
                        resultText += val;
                    else
                        resultText += ',';
                }

                sw.WriteLine(resultText);
            }
            
        }

        
    }

    public class CalibratedRetentionTimeRecord
    {
        public static CsvConfiguration CsvConfiguration = 
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {   
                Delimiter = ",",
                HasHeaderRecord = true,
            };
        public string FullSequence { get; set; }
        public Dictionary<string, double> AdjustedRetentionTimes { get; set; }

        public CalibratedRetentionTimeRecord(string fullSequence,
            List<(string FileName, double AdjustedRetentionTime)> adjustedRetentionTimes)
        {
            FullSequence = fullSequence;
            AdjustedRetentionTimes = adjustedRetentionTimes
                .ToDictionary(p => p.FileName, p => p.AdjustedRetentionTime);
        }
    }
}
