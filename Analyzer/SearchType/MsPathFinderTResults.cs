﻿using System.Collections;
using System.Diagnostics;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Util;
using Easy.Common.Extensions;
using MassSpectrometry;
using Readers;
using ThermoFisher.CommonCore.Data.Business;

namespace Analyzer.SearchType
{
    public class MsPathFinderTResults : BulkResult, IEnumerable<MsPathFinderTIndividualFileResult>, 
        IChimeraBreakdownCompatible, IDisposable
    {
        private string _datasetInfoFilePath => Path.Combine(DirectoryPath, "DatasetInfoFile.tsv");
        private string _crossTabResultFilePath;

        private bool _runBulk =>
            Override ||
            !File.Exists(_bulkResultCountComparisonPath) ||
            (new BulkResultCountComparisonFile(_bulkResultCountComparisonPath).First().ProteinGroupCount == 0 && !_crossTabResultFilePath.IsNullOrEmpty());

        private MsPathFinderTCrossTabResultFile _crossTabResultFile;
        public MsPathFinderTCrossTabResultFile CrossTabResultFile => _crossTabResultFile ??= new MsPathFinderTCrossTabResultFile(_crossTabResultFilePath);
        private string _combinedTargetResultFilePath => Path.Combine(DirectoryPath, "CombinedTargetResults_IcTarget.tsv");
        private MsPathFinderTResultFile? _combinedTargetResults;
        public MsPathFinderTResultFile CombinedTargetResults => _combinedTargetResults ??= CombinePrSMFiles();
        public List<MsPathFinderTIndividualFileResult> IndividualFileResults { get; set; }
        public MsPathFinderTResults(string directoryPath) : base(directoryPath)
        {
            IsTopDown = true;
            IndividualFileResults = new List<MsPathFinderTIndividualFileResult>();

            // combined file if ProMexAlign was ran
            _crossTabResultFilePath = Directory.GetFiles(DirectoryPath).FirstOrDefault(p => p.Contains("crosstab.tsv")); 

            // sorting out the individual result files
            var files = Directory.GetFiles(DirectoryPath)
                .Where(p => !p.Contains(".txt") && !p.Contains(".png") && !p.Contains(".db") && !p.Contains("Dataset"))
                .GroupBy(p => string.Join("_", Path.GetFileNameWithoutExtension(
                    p.Replace("_IcDecoy", "").Replace("_IcTarget", "").Replace("_IcTda", ""))))
                .ToDictionary(p => p.Key, p => p.ToList());
            foreach (var resultFile in files.Where(p => p.Value.Count == 6))
            {
                var key = resultFile.Key;
                var decoyPath = resultFile.Value.First(p => p.Contains("Decoy"));
                var targetPath = resultFile.Value.First(p => p.Contains("Target"));
                var combinedPath = resultFile.Value.First(p => p.Contains("IcTda"));
                var rawFilePath = resultFile.Value.First(p => p.Contains(".pbf"));
                var paramsPath = resultFile.Value.First(p => p.Contains(".param"));
                var ftFilepath = resultFile.Value.First(p => p.Contains(".ms1ft"));

                IndividualFileResults.Add(new MsPathFinderTIndividualFileResult(decoyPath, targetPath, combinedPath, key, ftFilepath, paramsPath, rawFilePath));
            }
            // TODO: Add case for the with mods search where not all items will be in the same directory
            foreach (var resultFile in files.Where(p => p.Value.Count == 4))
            {
                var key = resultFile.Key;
                var decoyPath = resultFile.Value.First(p => p.Contains("Decoy"));
                var targetPath = resultFile.Value.First(p => p.Contains("Target"));
                var combinedPath = resultFile.Value.First(p => p.Contains("IcTda"));
                var paramsPath = resultFile.Value.First(p => p.Contains(".param"));
                var rawFilePath = Directory.GetParent(directoryPath).GetDirectories("MsPathFinderT").First()
                    .GetFiles($"{key}.pbf").First().FullName;
                var ftPath = Directory.GetParent(directoryPath).GetDirectories("MsPathFinderT").First()
                    .GetFiles($"{key}.ms1ft").First().FullName;
                IndividualFileResults.Add(new MsPathFinderTIndividualFileResult(decoyPath, targetPath, combinedPath, key, ftPath, paramsPath, rawFilePath));
            }
        }

        /// <summary>
        /// Uses each individual target results file to count PrSMs, Proteins, and Proteoforms
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override BulkResultCountComparisonFile GetIndividualFileComparison(string path = null)
        {
            if (!Override && File.Exists(_IndividualFilePath))
                return new BulkResultCountComparisonFile(_IndividualFilePath);

            var results = new List<BulkResultCountComparison>();
            foreach (var file in IndividualFileResults)
            {
                var psmCount = file.TargetResults.Results.Count;
                var onePercentPsmCount = file.TargetResults.FilteredResults.Count;
                var proteoformCount = file.TargetResults.GroupBy(p => p,
                    CustomComparer<MsPathFinderTResult>.MsPathFinderTDistinctProteoformComparer).Count();
                var onePercentProteoformCount = file.TargetResults.FilteredResults.GroupBy(p => p,
                    CustomComparer<MsPathFinderTResult>.MsPathFinderTDistinctProteoformComparer).Count();
                var proteinCount = file.TargetResults.GroupBy(p => p,
                    CustomComparer<MsPathFinderTResult>.MsPathFinderTDistinctProteinComparer).Count();
                var onePercentProteinCount = file.TargetResults.FilteredResults.GroupBy(p => p,
                    CustomComparer<MsPathFinderTResult>.MsPathFinderTDistinctProteinComparer).Count();

                results.Add(new BulkResultCountComparison()
                {
                    Condition = Condition,
                    DatasetName = DatasetName,
                    FileName = file.Name,
                    OnePercentPsmCount = onePercentPsmCount,
                    PsmCount = psmCount,
                    PeptideCount = proteoformCount,
                    OnePercentPeptideCount = onePercentProteoformCount,
                    ProteinGroupCount = proteinCount,
                    OnePercentProteinGroupCount = onePercentProteinCount
                });
            }

            var bulkComparisonFile = new BulkResultCountComparisonFile(_IndividualFilePath)
            {
                Results = results
            };
            bulkComparisonFile.WriteResults(_IndividualFilePath);
            return bulkComparisonFile;
        }

        public override ChimeraCountingFile CountChimericPsms()
        {
            if (!Override && File.Exists(_chimeraPsmPath))
                return new ChimeraCountingFile(_chimeraPsmPath);

            var prsms = CombinedTargetResults.GroupBy(p => p, CustomComparer<MsPathFinderTResult>.MsPathFinderTChimeraComparer)
                .GroupBy(m => m.Count()).ToDictionary(p => p.Key, p => p.Count());
            var filtered = CombinedTargetResults.FilteredResults.GroupBy(p => p, CustomComparer<MsPathFinderTResult>.MsPathFinderTChimeraComparer)
                .GroupBy(m => m.Count()).ToDictionary(p => p.Key, p => p.Count());

            var results = prsms.Keys.Select(count => new ChimeraCountingResult(count, prsms[count],
                filtered.TryGetValue(count, out var psmCount) ? psmCount : 0, DatasetName, Condition)).ToList();
            _chimeraPsmFile = new ChimeraCountingFile() { FilePath = _chimeraPsmPath, Results = results };
            _chimeraPsmFile.WriteResults(_chimeraPsmPath);
            return _chimeraPsmFile;
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
        {
            if (!_runBulk)
                return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);
            
            int proteoformCount = 0;
            int onePercentProteoformCount = 0;
            int proteinCount = 0;
            int onePercentProteinCount = 0;
            if (!_crossTabResultFilePath.IsNullOrEmpty()) // if ProMexAlign was ran
            {
                proteoformCount = CrossTabResultFile.TargetResults.DistinctBy(p => p,
                        CustomComparer<MsPathFinderTCrossTabResultRecord>
                            .MsPathFinderTCrossTabDistinctProteoformComparer)
                    .Count();
                onePercentProteoformCount = CrossTabResultFile.FilteredTargetResults.DistinctBy(p => p,
                        CustomComparer<MsPathFinderTCrossTabResultRecord>
                            .MsPathFinderTCrossTabDistinctProteoformComparer)
                    .Count();

                proteinCount = CrossTabResultFile.TargetResults.SelectMany(p => p.ProteinAccession).Distinct().Count();
                onePercentProteinCount = CrossTabResultFile.FilteredTargetResults.SelectMany(p => p.ProteinAccession).Distinct().Count();
            }

            var result = new BulkResultCountComparison()
            {
                Condition = Condition,
                DatasetName = DatasetName,
                FileName = "Combined",
                OnePercentPsmCount = CombinedTargetResults.FilteredResults.Count,
                PsmCount = CombinedTargetResults.Results.Count,
                PeptideCount = proteoformCount,
                OnePercentPeptideCount = onePercentProteoformCount,
                ProteinGroupCount = proteinCount,
                OnePercentProteinGroupCount = onePercentProteinCount
            };

            var bulkComparisonFile = new BulkResultCountComparisonFile(_bulkResultCountComparisonPath)
            {
                Results = new List<BulkResultCountComparison> { result }
            };
            bulkComparisonFile.WriteResults(_bulkResultCountComparisonPath);
            return bulkComparisonFile;
        }

        public MsPathFinderTResultFile CombinePrSMFiles()
        {
            if (!Override && File.Exists(_combinedTargetResultFilePath))
                return new MsPathFinderTResultFile(_combinedTargetResultFilePath);

            var results = IndividualFileResults.SelectMany(p => p.TargetResults.Results).ToList();
            var file = new MsPathFinderTResultFile(_combinedTargetResultFilePath) { Results = results };
            file.WriteResults(_combinedTargetResultFilePath);
            return file;
        }

        public void CreateDatasetInfoFile()
        {
            if (File.Exists(_datasetInfoFilePath))
                return;
            using var sw = new StreamWriter(_datasetInfoFilePath);
            sw.WriteLine("Label\tRawFilePath\tMs1FtFilePath\tMsPathfinderIdFilePath");
            foreach (var individualFile in IndividualFileResults)
            {
                sw.WriteLine($"{individualFile.Name}\t{individualFile.PbfFilePath}\t{individualFile.Ms1FtFilePath}\t{individualFile.CombinedPath}");
            }
            sw.Dispose();
        }



        private string _chimeraBreakDownPath => Path.Combine(DirectoryPath, $"{DatasetName}_{Condition}_{FileIdentifiers.ChimeraBreakdownComparison}");
        private ChimeraBreakdownFile? _chimeraBreakdownFile;
        public ChimeraBreakdownFile ChimeraBreakdownFile => _chimeraBreakdownFile ??= GetChimeraBreakdownFile();

        public ChimeraBreakdownFile GetChimeraBreakdownFile()
        {
            if (!Override && File.Exists(_chimeraBreakDownPath))
                return new ChimeraBreakdownFile(_chimeraBreakDownPath);

            bool useIsolation;
            List<ChimeraBreakdownRecord> chimeraBreakDownRecords = new();

            // PrSMs
            foreach (var individualFileResult in IndividualFileResults)
            {
                useIsolation = true;
                MsDataFile dataFile = null;
                var dataFilePath = individualFileResult.PbfFilePath;
                if (dataFilePath is null)
                    useIsolation = false;
                else
                {
                    try
                    {
                        dataFile = MsDataFileReader.GetDataFile(dataFilePath);
                        dataFile.InitiateDynamicConnection();
                    }
                    catch
                    {
                        useIsolation = false;
                    }
                }

                // PrSMs
                foreach (var chimeraGroup in individualFileResult.CombinedResults.FilteredResults.GroupBy(p => p,
                             CustomComparer<MsPathFinderTResult>.MsPathFinderTChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension,
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().OneBasedScanNumber,
                        Type = Util.ResultType.Psm,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        PsmCharges = chimeraGroup.Select(p => p.Charge).ToArray(),
                        PsmMasses = chimeraGroup.Select(p => p.MonoisotopicMass).ToArray()
                    };

                    MsPathFinderTResult? parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        MsPathFinderTResult[] orderedChimeras;
                        if (useIsolation)
                        {
                            var ms2Scan =
                                dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().OneBasedScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null)
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            else 
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => Math.Abs(p.MostAbundantIsotopeMz - (double)isolationMz))
                                    .ThenBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else
                        {
                            orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                .OrderBy(p => p.EValue)
                                .ThenBy(p => p.Probability)
                                .ToArray();
                        }

                        foreach (var chimera in orderedChimeras)
                            if (parent is null)
                                parent = chimera;
                            else if (parent.BaseSequence == chimera.BaseSequence && parent.Modifications == chimera.Modifications)
                                record.DuplicateCount++;
                            else if (parent.Accession == chimera.Accession)
                                record.UniqueForms++;
                            else
                                record.UniqueProteins++;
                    }
                    chimeraBreakDownRecords.Add(record);
                }

                // unique proteoforms
                foreach (var chimeraGroup in individualFileResult.CombinedResults.FilteredResults.GroupBy(p => p,
                                 CustomComparer<MsPathFinderTResult>.MsPathFinderTDistinctProteoformComparer)
                             .Select(p => p.OrderBy(m => m.EValue).ThenByDescending(m => m.Probability).First())
                             .GroupBy(p => p, CustomComparer<MsPathFinderTResult>.MsPathFinderTChimeraComparer)
                             .Select(p => p.ToArray()))
                {
                    var record = new ChimeraBreakdownRecord()
                    {
                        Dataset = DatasetName,
                        FileName = chimeraGroup.First().FileNameWithoutExtension,
                        Condition = Condition,
                        Ms2ScanNumber = chimeraGroup.First().OneBasedScanNumber,
                        Type = Util.ResultType.Peptide,
                        IdsPerSpectra = chimeraGroup.Length,
                        TargetCount = chimeraGroup.Count(p => !p.IsDecoy),
                        DecoyCount = chimeraGroup.Count(p => p.IsDecoy),
                        PeptideCharges = chimeraGroup.Select(p => p.Charge).ToArray(),
                        PeptideMasses = chimeraGroup.Select(p => p.MonoisotopicMass).ToArray()
                    };

                    MsPathFinderTResult? parent = null;
                    if (chimeraGroup.Length != 1)
                    {
                        MsPathFinderTResult[] orderedChimeras;
                        if (useIsolation)
                        {
                            var ms2Scan =
                                dataFile.GetOneBasedScanFromDynamicConnection(chimeraGroup.First().OneBasedScanNumber);
                            var isolationMz = ms2Scan.IsolationMz;
                            if (isolationMz is null)
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            else
                                orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                    .OrderBy(p => Math.Abs(p.MostAbundantIsotopeMz - (double)isolationMz))
                                    .ThenBy(p => p.EValue)
                                    .ThenBy(p => p.Probability)
                                    .ToArray();
                            record.IsolationMz = isolationMz ?? -1;
                        }
                        else
                        {
                            orderedChimeras = chimeraGroup.Where(p => !p.IsDecoy)
                                .OrderBy(p => p.EValue)
                                .ThenBy(p => p.Probability)
                                .ToArray();
                        }

                        foreach (var chimera in orderedChimeras)
                            if (parent is null)
                                parent = chimera;
                            else if (parent.BaseSequence == chimera.BaseSequence && parent.Modifications == chimera.Modifications)
                                record.DuplicateCount++;
                            else if (parent.Accession == chimera.Accession)
                                record.UniqueForms++;
                            else
                                record.UniqueProteins++;
                    }
                    chimeraBreakDownRecords.Add(record);
                }
                if (useIsolation)
                    dataFile.CloseDynamicConnection();
            }

            var file = new ChimeraBreakdownFile(_chimeraBreakDownPath) { Results = chimeraBreakDownRecords };
            file.WriteResults(_chimeraBreakDownPath);
            return file;
        }

        public IEnumerator<MsPathFinderTIndividualFileResult> GetEnumerator()
        {
            return IndividualFileResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public new void Dispose()
        {
            base.Dispose();
            _chimeraBreakdownFile = null;
        }
    }

    public class MsPathFinderTIndividualFileResult
    {
        public string Name { get; set; }
        private string _targetPath;
        private MsPathFinderTResultFile _targetResults;
        public MsPathFinderTResultFile TargetResults => _targetResults ??= new MsPathFinderTResultFile(_targetPath);

        private string _decoyPath;
        private MsPathFinderTResultFile _decoyResults;
        public MsPathFinderTResultFile DecoyResults => _decoyResults ??= new MsPathFinderTResultFile(_decoyPath);

        internal string CombinedPath;
        private MsPathFinderTResultFile _combinedResults;
        public MsPathFinderTResultFile CombinedResults => _combinedResults ??= new MsPathFinderTResultFile(CombinedPath);


        public string Ms1FtFilePath { get; set; }
        public string ParamPath { get; set; }
        public string PbfFilePath { get; set; }
        public string RawFilePath { get; set; }

        public MsPathFinderTIndividualFileResult(string decoyPath, string targetPath, string combinedPath, string name, string ms1FtFilePath, string paramPath, string pbfFilePath)
        {
            _decoyPath = decoyPath;
            _targetPath = targetPath;
            CombinedPath = combinedPath;
            Name = name;
            Ms1FtFilePath = ms1FtFilePath;
            ParamPath = paramPath;
            PbfFilePath = pbfFilePath;

            string rawFileDirPath = pbfFilePath.Contains("Ecoli") ?
                @"B:\RawSpectraFiles\Ecoli_SEC_CZE" :
                @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\DataFiles";
            string rawPath = Path.Combine(rawFileDirPath, Path.GetFileNameWithoutExtension(pbfFilePath)+".raw");
            if (File.Exists(rawPath))
                RawFilePath = rawPath;
            else
                RawFilePath = pbfFilePath;
        }
    }
}
