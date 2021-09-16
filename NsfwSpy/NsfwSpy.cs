﻿using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace NsfwSpy
{
    /// <summary>
    /// The NsfwSpy classifier class used to analyse images for explicit content.
    /// </summary>
    public class NsfwSpy
    {
        private static ITransformer _model;

        public NsfwSpy()
        {
            if (_model == null)
            {
                var modelPath = Path.Combine(AppContext.BaseDirectory, "NsfwSpyModel.zip");
                var mlContext = new MLContext();
                _model = mlContext.Model.Load(modelPath, out var modelInputSchema);
            }
        }

        /// <summary>
        /// Classify an image from a byte array.
        /// </summary>
        /// <param name="imageData">The image content read as a byte array.</param>
        /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
        public NsfwSpyResult ClassifyImage(byte[] imageData)
        {
            var modelInput = new ModelInput(imageData);
            var mlContext = new MLContext();
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
            var modelOutput = predictionEngine.Predict(modelInput);
            return new NsfwSpyResult(modelOutput);
        }

        /// <summary>
        /// Classify an image from a file path.
        /// </summary>
        /// <param name="filePath">Path to the image to be classified.</param>
        /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
        public NsfwSpyResult ClassifyImage(string filePath)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var result = ClassifyImage(fileBytes);
            return result;
        }

        /// <summary>
        /// Classify an image from a web url.
        /// </summary>
        /// <param name="uri">Web address of the image to be classified.</param>
        /// <param name="webClient">A custom WebClient to download the image with.</param>
        /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
        public NsfwSpyResult ClassifyImage(Uri uri, WebClient webClient = null)
        {
            if (webClient == null) webClient = new WebClient();

            var fileBytes = webClient.DownloadData(uri);
            var result = ClassifyImage(fileBytes);
            return result;
        }

        /// <summary>
        /// Classify an image from a file path asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the image to be classified.</param>
        /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
        public async Task<NsfwSpyResult> ClassifyImageAsync(string filePath)
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var result = ClassifyImage(fileBytes);
            return result;
        }

        /// <summary>
        /// Classify an image from a web url asynchronously.
        /// </summary>
        /// <param name="uri">Web address of the image to be classified.</param>
        /// <param name="webClient">A custom WebClient to download the image with.</param>
        /// <returns>A NsfwSpyResult that indicates the predicted value and scores for the 5 categories of classification.</returns>
        public async Task<NsfwSpyResult> ClassifyImageAsync(Uri uri, WebClient webClient = null)
        {
            if (webClient == null) webClient = new WebClient();

            var fileBytes = await webClient.DownloadDataTaskAsync(uri);
            var result = ClassifyImage(fileBytes);
            return result;
        }

        /// <summary>
        /// Classify multiple images from a list of file paths.
        /// </summary>
        /// <param name="filesPaths">Collection of file paths to be classified.</param>
        /// <param name="actionAfterEachClassify">Action to be invoked after each file is classified.</param>
        /// <returns>A list of results with their associated file paths.</returns>
        public List<NsfwSpyValue> ClassifyImages(IEnumerable<string> filesPaths, Action<string, NsfwSpyResult> actionAfterEachClassify = null)
        {
            var results = new List<NsfwSpyValue>();

            Parallel.ForEach(filesPaths, new ParallelOptions { MaxDegreeOfParallelism = 4 }, filePath =>
            {
                var result = ClassifyImage(filePath);
                var value = new NsfwSpyValue(filePath, result);
                results.Add(value);

                if (actionAfterEachClassify != null)
                    actionAfterEachClassify.Invoke(filePath, result);
            });

            return results;
        }
    }
}