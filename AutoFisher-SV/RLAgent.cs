using System;
using System.Collections;
using NumSharp;
using ArtificialNeuralNetwork;
using NeuralNetwork.Backpropagation;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;

namespace fishing
{
    class RLAgent


    
    {

        private InferenceSession session;
        const string modelPath = "Mods/AutoFisher-SV/assets/policy_net.onnx";


        public RLAgent()
        {



            SessionOptions options = new SessionOptions();
            options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
            options.AppendExecutionProvider_CPU(1);



            // Run inference
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
        
            using (var results = session.Run(inputs))
            {
                var maxValue = results.Max();
                var maxIndex = results.ToList().IndexOf(maxValue);
                return maxIndex;
            }
            

        }




    }
}
