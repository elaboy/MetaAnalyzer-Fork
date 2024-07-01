﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using TaskLayer;

namespace ChimericSpectrumConverter
{
    internal class ChimericSpectrumConverterParameters : BaseResultAnalyzerTaskParameters
    {
        public List<string> SpectraFiles { get; }
        public string OutputDirectory { get; }
        public DeconvolutionParameters PrecursorDeconvolutionParameters { get; }

        public ChimericSpectrumConverterParameters(string inputDirectoryPath, string outputDirectory, 
            List<string> spectraFiles, int maxChargeState, bool overrideFiles, bool runOnAll) 
            : base(inputDirectoryPath, overrideFiles, runOnAll)
        {
            OutputDirectory = outputDirectory;
            SpectraFiles = spectraFiles;
            PrecursorDeconvolutionParameters = new ClassicDeconvolutionParameters(1, maxChargeState, 10, 3);
        }
    }
}
