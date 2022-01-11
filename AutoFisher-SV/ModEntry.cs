
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.IO;
using System.Collections.Generic;

namespace fishing
{


    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {

        private int CountFishes = 0;
        private bool IsFishing = false;

        // INITIALIZE STATE

        // this is the bar, the lower end 
        float bobberBarPos = 0;
        // velocity of bar
        float bobberBarSpeed = 0;
        // this is the fish 
        float bobberPosition = 15;
        // reward
        // from 0 to 1
        float distanceFromCatching = 0;

        // store model's last state between updates
        double[] stateBuffer = { 0, 0, 0, 0 };
        // store model's last action between updates
        bool actionBuffer = false;


        public double[,] replayMemory;

        public int updateCounter = 0;

        public bool autoCastRod = true;

        Random rnd = new Random();

        private RLAgent Agent;

        const string datasetFile = "replayMemory.csv";

        const int bufferSize = 200;

        const bool shouldStoreDataset = false;

        /*********
        ** Public methods
        *********/


        /// <summary>
        /// Check if the player is waiting for the caught fish popup to go away
        /// </summary>
        /// <param name="currentTool">A FishingRod that is being used to fish</param>
        /// <returns>true if the mod should click the popup away</returns>
        private bool ShouldDoDismissCaughtPopup(FishingRod fr)
        {
            return (!Context.CanPlayerMove) && fr.fishCaught && fr.inUse() && !fr.isCasting && !fr.isFishing &&
                   !fr.isReeling && !fr.isTimingCast && !fr.pullingOutOfWater && !fr.showingTreasure;
        }




        /// <summary>
        /// Test whether the mod can automatically cast the fishing rod for the user.
        /// </summary>
        /// <param name="currentTool">A FishingRod that is being used to fish</param>
        /// <returns>true if the mod should cast for the user</returns>
        private bool ShouldDoAutoCast(FishingRod fr)
        {
            return Context.CanPlayerMove && Game1.activeClickableMenu is null && !fr.castedButBobberStillInAir &&
                   !fr.hit && !fr.inUse() && !fr.isCasting && !fr.isFishing && !fr.isNibbling && !fr.isReeling &&
                   !fr.isTimingCast && !fr.pullingOutOfWater;
        }




        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {

            replayMemory = new double[bufferSize, 8];

            if (!File.Exists(datasetFile))
            {
                File.CreateText(datasetFile);
                this.Monitor.Log("Created Dataset File" + helper.ModRegistry.ModID, LogLevel.Info);

            }
            else
            {
                this.Monitor.Log("Dataset File Already exists" + helper.ModRegistry.ModID, LogLevel.Info);
            }

            // apply clicking hack
            this.Monitor.Log("Added Patches for Mod " + helper.ModRegistry.ModID, LogLevel.Info);
            MyPatcher.DoPatching();


            Agent = new RLAgent();

            this.Monitor.Log("Loading event handlers", LogLevel.Trace);

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
            helper.Events.Display.MenuChanged += OnMenuChanged;

        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>

        private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            Farmer player = Game1.player;
            if (player == null || !player.IsLocalPlayer)
            {
                return;
            }

            if (args.NewMenu is BobberBar bar)
            {

                CountFishes++;

                //log.Log($"Fish #{CountFishes}");

                IsFishing = true;

                // No treasures to mess with training!
                Helper.Reflection.GetField<bool>(bar, "treasure").SetValue(false);

            }

            if (args.NewMenu is ItemGrabMenu menu)
            {
                // JUST... JUST LEAVE ME BE
                menu.exitThisMenu();

            }

        }

        private void OnInventoryChanged(object sender, InventoryChangedEventArgs args)
        {
            Farmer player = args.Player;

            foreach (Item item in args.Added)
            {
                player.Items.Remove(item);
            }


        }


        private void OnUpdateTicked(object sender, UpdateTickedEventArgs args)
        {


            Farmer player = Game1.player;


            // freeze time
            Game1.gameTimeInterval = 0;

            // infinite stamina
            player.stamina = player.MaxStamina;


            if (player == null || !player.IsLocalPlayer)
                return;
            if (!(Game1.player.CurrentTool is FishingRod))
                return;

            FishingRod rod = Game1.player.CurrentTool as FishingRod;



            if (autoCastRod & ShouldDoAutoCast(rod))
            {
                rod.beginUsing(Game1.currentLocation,
                                 Game1.player.getStandingX(),
                                 Game1.player.getStandingY(),
                                 Game1.player);
            }


            if (rod.isTimingCast)
            {
                rod.castingTimerSpeed = 0;
                rod.castingPower = 1;
            }

            if (!rod.isNibbling && rod.isFishing && !rod.isReeling && !rod.pullingOutOfWater && !rod.hit)
            {
                rod.timeUntilFishingBite = 0;
            }

            if (rod.isNibbling && rod.isFishing && !rod.isReeling && !rod.pullingOutOfWater && !rod.hit)
            {
                Farmer.useTool(player);
            }

            // Click away the catched fish.  The conditionals are ordered here in a way
            // the check for the popup happens before the config check so the code can
            // always check the treasure chest.  See the variable doneCaughtFish for more
            // info.
            if (ShouldDoDismissCaughtPopup(rod))
            {
                //log.Trace("Tool is sitting at caught fish popup");
                //log.Trace("Closing popup with Harmony");
                ClickAtAllHack.simulateClick = true;

            }



            if (args.IsMultipleOf(5))
            {

                //IsButtonDownHack.simulateDown = false;

                if (Game1.activeClickableMenu is BobberBar bar)
                {


                    int best_action;

                    double diffBobberFish = bobberPosition - bobberBarPos;

                    // get values from last time update ran
                    double[] OldState = new double[] { (double)bobberBarPos, (double)bobberPosition, (double)bobberBarSpeed};

                    // if is the first iteration StateBuffer don't have anything
                    if (CountFishes == 0)
                    {
                        stateBuffer = OldState;
                    }


                    // Update State
                    bobberBarPos = Helper.Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                    bobberBarSpeed = Helper.Reflection.GetField<float>(bar, "bobberBarSpeed").GetValue();
                    bobberPosition = Helper.Reflection.GetField<float>(bar, "bobberPosition").GetValue();
                    distanceFromCatching = Helper.Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();


                    diffBobberFish = bobberPosition - bobberBarPos;

                    double[] NewState = new double[] { (double)bobberBarPos, (double)bobberPosition, (double)bobberBarSpeed};

                    // detect if mouse is currently pressed
                    bool action = this.Helper.Input.IsDown(SButton.MouseLeft);
                    double reward = distanceFromCatching; 

                    int rand = rnd.Next(100);

                    best_action = (int) Agent.Update(NewState);

                    // execute action if needed
                    if (best_action==0)
                    {
                        IsButtonDownHack.simulateDown = true;
                    }
                    else
                    {
                        IsButtonDownHack.simulateDown = false;

                    }


                    // store last state
                    stateBuffer = OldState;
                     

                    
                    // [1000][8]
                    // <S_t-1, S_t, a_t-1, r_t> -> a_t
                    // state from last update
                    replayMemory[updateCounter,0] = OldState[0];
                    replayMemory[updateCounter,1] = OldState[1];
                    replayMemory[updateCounter,2] = OldState[2];
                    // state from this update
                    replayMemory[updateCounter,3] = NewState[0];
                    replayMemory[updateCounter,4] = NewState[1];
                    replayMemory[updateCounter,5] = NewState[2];
                    // reward from this update
                    replayMemory[updateCounter,6] = reward;
                    // action from last update
                    replayMemory[updateCounter,7] = actionBuffer? 1 : 0;

                    // if memory is full dump everything on csv file and reset memory 
                    if (updateCounter == bufferSize - 1 && shouldStoreDataset)
                    {
                        List<string> iterRows = new List<string>();

                        for (int i = 0; i < bufferSize; i++)
                        {
                            
                            string csvRow = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
                                                                                replayMemory[i,0],
                                                                                replayMemory[i,1],
                                                                                replayMemory[i,2],
                                                                                replayMemory[i,3],
                                                                                replayMemory[i,4],
                                                                                replayMemory[i,5],
                                                                                replayMemory[i,6],
                                                                                replayMemory[i,7]); 
                            iterRows.Add(csvRow);
                        }
                        

                        File.AppendAllLines(datasetFile, iterRows);

                        this.Monitor.Log("Dumped 200 entries in dataset" + this.Helper.ModRegistry.ModID,LogLevel.Info);
                        updateCounter=0;
                    }
                    else
                    {
                        updateCounter++;
                    }


                    // store last action
                    actionBuffer = action;
                }


            }

        }










    }
}