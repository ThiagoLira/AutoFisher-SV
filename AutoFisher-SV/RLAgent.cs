using System;
using System.Collections;
using NumSharp;
using ArtificialNeuralNetwork;
using NeuralNetwork.Backpropagation;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;
using StardewModdingAPI;

namespace fishing
{
    class RLAgent


    
    {

        private InferenceSession session;
        const string modelPath = "Mods/AutoFisher-SV/assets/policy_net.onnx";
        private IMonitor logger;

        public RLAgent(IMonitor monitor)
        {

            logger = monitor;

            SessionOptions options = new SessionOptions();
            options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
            options.AppendExecutionProvider_CPU(1);



            // Run inference
            session = new InferenceSession(modelPath, options);


        }


        public void reloadModel()
        {
            this.logger.Log("Model RELOADED", StardewModdingAPI.LogLevel.Trace);
            SessionOptions options = new SessionOptions();
            options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
            options.AppendExecutionProvider_CPU(1);
            session = new InferenceSession(modelPath, options);
        }

        public int Update(double[] currentState)
        {

            Tensor<double> input = new DenseTensor<double>(new[] {3});

            input[0] = currentState[0];
            input[1] = currentState[1];
            input[2] = currentState[2];

            // Setup inputs and outputs
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor<double>("0", input)
            };


            // this.logger.Log("NN input: " + string.Join(", ", input.ToList()), StardewModdingAPI.LogLevel.Trace);

            using (var results = session.Run(inputs))
            {

                Tensor<double> outputs = results.First().AsTensor<double>();

                // this.logger.Log("NN output: " + string.Join(", ", outputs.ToList()), StardewModdingAPI.LogLevel.Trace);

                var maxValue = outputs.Max();
                var maxIndex = outputs.ToList().IndexOf(maxValue);
                // this.logger.Log("Taking action: " + maxIndex, StardewModdingAPI.LogLevel.Trace);

                return maxIndex;
            }
            

        }




    }
}
