﻿using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using UsefulProteomicsDatabases;

namespace Test
{
    internal class TopDownRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;
        private static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath);

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }

      

        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    //result.Override = true;
                    result.CountChimericPsms();
                    result.GetBulkResultCountComparisonFile();
                    result.GetIndividualFileComparison();
                    if (result is IChimeraBreakdownCompatible cb)
                        cb.GetChimeraBreakdownFile();
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
                    if (result is MetaMorpheusResult mm)
                    {
                        mm.PlotPepFeaturesScatterGrid();
                        mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                    }
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;

                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                cellLine.PlotModificationDistribution(ResultType.Psm, false);
                cellLine.PlotModificationDistribution(ResultType.Peptide, false);

                cellLine.Dispose();
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();

        }

        [Test]
        public static void GenerateAllFigures()
        {
            //foreach (CellLineResults cellLine in AllResults)
            //{
            //    foreach (var individualResult in cellLine)
            //    {
            //        if (individualResult is not MetaMorpheusResult mm) continue;
            //        //mm.PlotPepFeaturesScatterGrid();
            //        //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
            //    }

            //    cellLine.PlotIndividualFileResults();
            //    cellLine.PlotCellLineSpectralSimilarity();
            //    cellLine.PlotCellLineChimeraBreakdown();
            //    cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            //}

            AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparisons();
            AllResults.PlotStackedIndividualFileComparison();
            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }

        [Test]
        public static void GenerateSpecificFigures()
        {
            var a549 = BottomUpRunner.AllResults.First();
            a549.PlotModificationDistribution();
            a549.PlotModificationDistribution(ResultType.Peptide);
            var jurkat = AllResults.Skip(1).First();
            jurkat.PlotModificationDistribution();
            jurkat.PlotModificationDistribution(ResultType.Peptide);
            //a549.PlotAccuracyByModificationType();
            //a549.PlotChronologerDeltaKernelPDF();
        }


        [Test]
        public static void MsPathTDatasetInfoGenerator()
        {
            foreach (MsPathFinderTResults mspt in AllResults.SelectMany(p => p.Results)
                .Where(p => p is MsPathFinderTResults mspt && mspt.IndividualFileResults.Count is 20 or 43 or 10))
            {
                mspt.CreateDatasetInfoFile();
            }
        }



        /// <summary>
        /// Overnight I will:
        /// Rerun all individual file results for MM due to the unique by base sequence issue
        /// Replot all individual file results
        ///
        /// Run get maximum chimera estimation file
        /// rerun every plot with new selectors
        /// </summary>
        [Test]
        public static void OvernightRunner()
        {
            //foreach (var cellLine in AllResults )
            //{
            //    foreach (var result in cellLine)
            //    {
            //        if (result is MetaMorpheusResult mm)
            //        {
            //            if (cellLine.GetSingleResultSelector().Contains(mm.Condition))
            //            {
            //                mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
            //                mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
            //                mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
            //                mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
            //            }
            //        }
            //    }
            //    //cellLine.PlotModificationDistribution();
            //    //cellLine.PlotModificationDistribution(ResultType.Peptide);

            //}
           
            foreach (var cellLine in BottomUpRunner.AllResults)
            {
                //cellLine.PlotModificationDistribution();
                //cellLine.PlotModificationDistribution(ResultType.Peptide);
                //cellLine.PlotCellLineRetentionTimeVsChronologerPredictionBubble();
                //foreach (var result in cellLine)
                //{
                //    if (result is MetaMorpheusResult mm)
                //    {
                //        if (Selector.BottomUpMann11Selector.SingleResultSelector.Contains(mm.Condition))
                //        {
                //            mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                //            mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                //            mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                //            mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                //        }
                //    }
                //}
            }

            BottomUpRunner.RunAllParsing();
            TopDownRunner.RunAllParsing();
            //foreach (var cellLine in AllResults)
            //{
            //    foreach (var result in cellLine.Where(p => true.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition)))
            //    {
            //        (result as MetaMorpheusResult)?.GetChimeraBreakdownFile();
            //    }
            //    // These require the masses and charges
            //    //cellLine.PlotChimeraBreakdownByMassAndCharge();
            //    cellLine.Dispose();
            //}



            //foreach (var cellLine in BottomUpRunner.AllResults)
            //{
            //    //cellLine.PlotChronologerDeltaPlotBoxAndWhisker();
            //    //cellLine.PlotChronologerDeltaRangePlot();

            //    foreach (var result in cellLine.Where(p => false.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition)))
            //    {
            //        (result as MetaMorpheusResult)?.GetChimeraBreakdownFile();
            //    }
            //    // These require the masses and charges
            //    cellLine.PlotChimeraBreakdownByMassAndCharge();


            //    //cellLine.Override = true;
            //    //cellLine.GetMaximumChimeraEstimationFile(false);
            //    //cellLine.Override = false;
            //    //cellLine.PlotAverageRetentionTimeShiftPlotKernelPdf(false);
            //    //cellLine.PlotAverageRetentionTimeShiftPlotHistogram(false);
            //    //cellLine.PlotAllRetentionTimeShiftPlots(false);
            //    cellLine.Dispose();
            //}
            ////BottomUpRunner.AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            ////BottomUpRunner.AllResults.PlotGridChronologerDeltaPlotKernalPDF();


        }
    


    

        [Test]
        public static void IsabellaData()
        {
            string path = @"B:\Users\AlexanderS_Bison\240515_DataFromITW";
            var results = (from dirpath in Directory.GetDirectories(path)
                    where !dirpath.Contains("Fig")
                    where dirpath.Contains("Ecoli")
                    select new MetaMorpheusResult(dirpath))
                .Cast<SingleRunResults>()
                .ToList();


            var cellLine = new CellLineResults(path, results);
            cellLine.Override = true;
            cellLine.GetBulkResultCountComparisonFile();
            cellLine.GetIndividualFileComparison();
            cellLine.GetBulkResultCountComparisonMultipleFilteringTypesFile();
            cellLine.PlotIndividualFileResults(ResultType.Psm, null, false);
            cellLine.PlotIndividualFileResults(ResultType.Peptide, null, false);
            cellLine.PlotIndividualFileResults(ResultType.Protein, null, false);
        }


    }
}
