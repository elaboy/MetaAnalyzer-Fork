﻿using System.Collections;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;

namespace Analyzer.SearchType
{
    public class AllResults : IEnumerable<DatasetResults>
    {
        public string DirectoryPath { get; set; }
        public bool Override { get; set; } = false;
        public List<DatasetResults> CellLineResults { get; set; }

        public AllResults(string directoryPath)
        {
            DirectoryPath = directoryPath;
            CellLineResults = new List<DatasetResults>();
            foreach (var directory in Directory.GetDirectories(DirectoryPath).Where(p => !p.Contains("Figures") && !p.Contains("Order"))) 
            {
                CellLineResults.Add(new DatasetResults(directory));
            }
        }

        public AllResults(string directoryPath, List<DatasetResults> cellLineResults)
        {
            DirectoryPath = directoryPath;
            CellLineResults = cellLineResults;
        }

        private string _chimeraCountingPath => Path.Combine(DirectoryPath, $"All_PSM_{FileIdentifiers.ChimeraCountingFile}");
        private ChimeraCountingFile _chimeraCountingFile;
        public ChimeraCountingFile ChimeraCountingFile => _chimeraCountingFile ??= CountChimericPsms();
        public ChimeraCountingFile CountChimericPsms()
        {
            if (!Override && File.Exists(_chimeraCountingPath))
                return new ChimeraCountingFile(_chimeraCountingPath);

            List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
            foreach (var cellLineResult in CellLineResults)
            {
                results.AddRange(cellLineResult.ChimeraCountingFile.Results);
            }

            var chimeraCountingFile = new ChimeraCountingFile(_chimeraCountingPath) { Results = results };
            chimeraCountingFile.WriteResults(_chimeraCountingPath);
            return chimeraCountingFile;
        }

        private string _chimeraPeptidePath => Path.Combine(DirectoryPath, $"All_Peptide_{FileIdentifiers.ChimeraCountingFile}");
        private ChimeraCountingFile _chimeraPeptideFile;
        public ChimeraCountingFile ChimeraPeptideFile => _chimeraPeptideFile ??= CountChimericPeptides();
        public ChimeraCountingFile CountChimericPeptides()
        {
            if (!Override && File.Exists(_chimeraPeptidePath))
                return new ChimeraCountingFile(_chimeraPeptidePath);

            List<ChimeraCountingResult> results = new List<ChimeraCountingResult>();
            foreach (var cellLineResult in CellLineResults)
            {
                results.AddRange(cellLineResult.ChimeraPeptideFile.Results);
            }

            var chimeraPeptideFile = new ChimeraCountingFile(_chimeraPeptidePath) { Results = results };
            chimeraPeptideFile.WriteResults(_chimeraPeptidePath);
            return chimeraPeptideFile;
        }

        private string _chimeraBreakdownFilePath => Path.Combine(DirectoryPath, $"All_{FileIdentifiers.ChimeraBreakdownComparison}");
        private ChimeraBreakdownFile _chimeraBreakdownFile;
        public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();

        public ChimeraBreakdownFile GetChimeraBreakdownFile()
        {
            if (!Override && File.Exists(_chimeraBreakdownFilePath))
                return new ChimeraBreakdownFile(_chimeraBreakdownFilePath);
            
            List<ChimeraBreakdownRecord> results = new List<ChimeraBreakdownRecord>();
            foreach (var bulkResult in CellLineResults)
            {
                results.AddRange(bulkResult.ChimeraBreakdownFile.Results);
            }

            var chimeraBreakdownFile = new ChimeraBreakdownFile(_chimeraBreakdownFilePath) { Results = results };
            chimeraBreakdownFile.WriteResults(_chimeraBreakdownFilePath);
            return chimeraBreakdownFile;
        }

        public string _bulkResultCountComparisonPath => Path.Combine(DirectoryPath, $"All_{FileIdentifiers.BottomUpResultComparison}");
        private BulkResultCountComparisonFile _bulkResultCountComparisonFile;
        public BulkResultCountComparisonFile BulkResultCountComparisonFile => _bulkResultCountComparisonFile ??= GetBulkResultCountComparisonFile();

        public BulkResultCountComparisonFile GetBulkResultCountComparisonFile()
        {
            if (!Override && File.Exists(_bulkResultCountComparisonPath))
                return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);

            List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
            foreach (var cellLineResult in CellLineResults)
            {
                results.AddRange(cellLineResult.BulkResultCountComparisonFile.Results);
            }

            var bulkResultCountComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath) { Results = results };
            bulkResultCountComparisonFile.WriteResults(_bulkResultCountComparisonPath);
            return bulkResultCountComparisonFile;
        }

        private string _individualFileComparisonPath => Path.Combine(DirectoryPath, $"All_{FileIdentifiers.IndividualFileComparison}");
        private BulkResultCountComparisonFile _individualFileComparison;
        public BulkResultCountComparisonFile IndividualFileComparisonFile => _individualFileComparison ??= IndividualFileComparison();

        public BulkResultCountComparisonFile IndividualFileComparison()
        {
            if (!Override && File.Exists(_individualFileComparisonPath))
                return new BulkResultCountComparisonFile(_individualFileComparisonPath);

            List<BulkResultCountComparison> results = new List<BulkResultCountComparison>();
            foreach (var cellLineResult in CellLineResults.Where(p => p.IndividualFileComparisonFile != null))
            {
                results.AddRange(cellLineResult.IndividualFileComparisonFile.Results);
            }

            var individualFileComparison = new BulkResultCountComparisonFile(_individualFileComparisonPath) { Results = results };
            individualFileComparison.WriteResults(_individualFileComparisonPath);
            return individualFileComparison;
        }

        public IEnumerator<DatasetResults> GetEnumerator()
        {
            return CellLineResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
