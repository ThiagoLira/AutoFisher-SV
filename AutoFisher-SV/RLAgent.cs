using System;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using NumSharp;
using ArtificialNeuralNetwork;
using NeuralNetwork.Backpropagation;

namespace fishing


{
    class RLAgent

    {

        // the AI can CLICK or NOT CLICK
        private const int nActions = 2;

        private const int stateSize = 3;

        // n_input, n_output, n_layers, dim_hidden
        private INeuralNetwork targetNetwork;
        private INeuralNetwork predictionNetwork;

        private const double bobberBarPosMax = 432d;
        private const double bobberBarPosMin = 0;

        private const double bobberBarSpeedMax = 16.8;
        private const double bobberBarSpeedMin = -16.8;

        private const double bobberPositionMax = 526.001d;
        private const double bobberPositionMin = 0;

        private const double distVictoryMax = 1;
        private const double distVictoryMin = 0;

        // new variable BobberPos - BobberBarPos 
        private const double diffBobberFishMax = 508.0d;
        private const double diffBobberFishMin = -432.0d;



        // Q-learning settings

        private const double epsilon = 0.3F;

        private const float learningRate = 0.6F;

        private const float discount = 0.2F;

        private const int stateMemorySize = 1000;

        // number of iterations to copy prediction network parameters to target network
        private const int C = 50;

        private Stack stateMemory;

        private int numItersElapsed;

        Random random;


        public void ReadSerializedNetwork()
        {
          // read network from serialized


        }
        public void DumpSerializedNetwork()
        {
            // dump network from serialized

        }

        public RLAgent()
        {
            targetNetwork = ArtificialNeuralNetwork.Factories.NeuralNetworkFactory.GetInstance().Create(stateSize, nActions, 1, 5);
            predictionNetwork = ArtificialNeuralNetwork.Factories.NeuralNetworkFactory.GetInstance().Create(stateSize, nActions, 1, 5);

            stateMemory = new Stack();
            random = new Random();


        }


        public int Update(double[] olderState, double[] oldState, double[] newState)
        {

            numItersElapsed++;

            int bestAction;

            // simple difference of winning bar height
            double reward = oldState[3] - olderState[3];

            // forward propagate on network
            predictionNetwork.SetInputs(newState);
            targetNetwork.SetInputs(newState);

            double[] predictionOutput = predictionNetwork.GetOutputs();
            // output is of the form [Q_value_action_0, Q_value_action_1]
            // with epsilon probability we get a random action from predictionOutput
            // otherwise we get the action corresponding with the maximum Q-value sampled by the network
            if (random.NextDouble() > epsilon)
            {
                bestAction = (int) (random.NextDouble() * 2.0);
            }
            else
            {
                bestAction = np.argmax(predictionOutput);
            }
            
            double[] targetOutput = targetNetwork.GetOutputs();


            // store memory < s,a,r,s’>
            //(double[], double[], int, double) state_tuple = (predictionOutput,targetOutput,bestAction,reward);
            stateMemory.Push(predictionOutput);
           
            if (numItersElapsed > C)
            {
                // copy prediction network to target network
                numItersElapsed = 0;
            }

            BackPropagateNetwork();

      
            return bestAction;

        }

        public void BackPropagateNetwork() 
        {

            double[] outputs = stateMemory.Pop();

            Backpropagater bp = new Backpropagater(predictionNetwork, 1e-2, 1,1, false);
            bp.Backpropagate(outputs);
        }



    }
}
