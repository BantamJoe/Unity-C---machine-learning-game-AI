using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DisruptorUnity3d;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using UnityEngine.UI;

public class ds736BLTemplate : BasicBehaviourLibrary {
    private int framesPerMsg = 4;
    private int frameCounter = 1;
    private NetworkToServer networkInstance = new NetworkToServer();
    private bool move = false;
    private string teamCol;
    private string prevState = "";
    private string prevScore = "";
    private string prevMoveMess = "";
    private string prevActionMess = "";
    private string prevMess = "";
    private bool completedAction = true;
    private int actionCounter = 0;
    private int moveCounter = 0;
    private bool completeAction = true;
    private bool completeMove = true;
    private bool sentCompleteAction = false;
    private bool sentCompleteMove = false;
    private int completeCounter = 0;
    private int prevNodeCount;


    private bool knownIsDead = false;
    private bool knownTookDamageRecently = false;
    private bool knownHasFlag = false;
    private bool knownSeenFlag = false;
    private bool knownEnemyFlagTaken = false;

    private int lowHealth = 30;

    private int killEnemyReward = 2;
    private int pickUpFlagReward = 3;
    private int deathReward = -5;
    private int tookDamageReward = -1;
    private int seeFlagReward = 1;

    /*
     * Function: MovetoFlag
     * ----------------------------
     *   A method for moving to the enemy flag, if it is in sight
     * 
     */
    public void MovetoFlag()
    {
        if (!EnemyTeamFlagInSight()) return;
        LookAt(EnemyFlagInSight.GetLocation());
        MoveTowards(EnemyFlagInSight.GetLocation());
    }

    /*
     * Function: FriendlyTeamFlagInSight
     * ----------------------------
     *   A method to check if the friendly flag is in sight
     * 
     *   return: a boolean saying if the friendly flag is in sight
     * 
     */
    public bool FriendlyTeamFlagInSight()
    {
        return FriendlyFlagInSight != null;
    }

    /*
     * Function: FriendlySpotted
     * ----------------------------
     *   A method to check if there are any friendlies within the sight of the agent
     * 
     *   return: a boolean saying if there are any friendlies within the sight of the agent
     * 
     */
    public bool FriendlySpotted()
    {
        return FriendlyAgentsInSight.Count > 0;
    }

    /*
     * Function: ShootEnemiesInSightWReward
     * ----------------------------
     *   Attempts to shoot the first enemy in sight, and returns reward for killing them
     * 
     *   return: a integer being the reward value of the action
     * 
     */
    public int ShootEnemiesInSightWReward()
    {
        if (!EnemiesSpotted())
            return 0;

        IAgent targetSoldier = EnemyAgentsInSight[0];
        // Any enemy soldiers in sight?
        if (targetSoldier != null)
        {
            // Look at them, if we are, shoot.
            if (LookAt(targetSoldier.GetLocation()))
            {
                // pew pew
                Shoot();
                if (targetSoldier.IsDead())
                {
                    return killEnemyReward;
                }
            }
        }
        return 0;
    }

    /*
     * Function: ShootLowestHealthEnemiesInSightWReward
     * ----------------------------
     *   Attempts to shoot the enemy with the lowest health in sight, and returns reward for killing them
     * 
     *   return: a integer being the reward value of the action
     * 
     */
    public int ShootLowestHealthEnemiesInSightWReward()
    {
        if (!EnemiesSpotted())
            return 0;

        int lowestHealth = 100;
        IAgent lowestHealthSoldier = EnemyAgentsInSight[0];
        foreach (IAgent targetSoldier in EnemyAgentsInSight)
        {
            if (targetSoldier.GetHealth() < lowestHealth)
            {
                lowestHealth = targetSoldier.GetHealth();
                lowestHealthSoldier = targetSoldier;
            }
        }

        // Look at them, if we are, shoot.
        if (LookAt(lowestHealthSoldier.GetLocation()))
        {
            // pew pew
            Shoot();
            if (lowestHealthSoldier.IsDead())
            {
                return killEnemyReward;
            }
        }

        return 0;
    }

    /*
     * Function: GetAgentHealth
     * ----------------------------
     *   A method to get the health of the agent
     * 
     *   return: a integer representing the health of the agent
     * 
     */
    public int GetAgentHealth()
    {
        return Health;
    }

    /*
     * Function: GetAgentLowHealth
     * ----------------------------
     *   Checks if the agent has low health or not
     * 
     *   return: a boolean of if the agent has low health
     * 
     */
    public bool GetAgentLowHealth()
    {
        if (GetAgentHealth() <= lowHealth) return true;
        return false;
    }

    /*
     * Function: GetLowestHealthEnemy
     * ----------------------------
     *   A method to get the health of the lowest health enemy within sight
     * 
     *   return: a integer of the health of the lowest health enemy within sight
     * 
     */
    public int GetLowestHealthEnemy()
    {
        if (!EnemiesSpotted())
            return 0;

        int lowestHealth = 100;

        foreach (IAgent targetSoldier in EnemyAgentsInSight){
            if (targetSoldier.GetHealth() < lowestHealth) lowestHealth = targetSoldier.GetHealth();
        }

        return lowestHealth;
    }

    /*
     * Function: GetEnemyHealthInSight
     * ----------------------------
     *   A function to get the total health of all enemies within sight
     * 
     *   return: a integer of the total health of all enemies within sight
     * 
     */
    public int GetEnemyHealthInSight()
    {
        if (!EnemiesSpotted())
            return 0;

        int totalHealth = 0;

        foreach (IAgent targetSoldier in EnemyAgentsInSight)
        {
            totalHealth += targetSoldier.GetHealth();
        }

        return totalHealth;
    }

    /*
     * Function: TotalEnemyHealthHigherThanAgent
     * ----------------------------
     *   A method to check if the total health of all enemies in sight is higher than the agents health
     * 
     *   return: a boolean that checks if the total health of all enemies in sight is higher than the agents health
     * 
     */
    public bool TotalEnemyHealthHigherThanAgent()
    {
        if (GetEnemyHealthInSight() > GetAgentHealth()) return true;
        return false;
    }

    /*
     * Function: LowestHealthEnemyHigherThanAgent
     * ----------------------------
     *   A method that checks if the lowest health enemy in sight, has more health than the agent
     * 
     *   return: a boolean if the lowest health enemy in sight, has more health than the agent
     * 
     */
    public bool LowestHealthEnemyHigherThanAgent()
    {
        if (GetLowestHealthEnemy() > GetAgentHealth()) return true;
        return false;
    }

    /*
     * Function: NumbEnemiesSpotted
     * ----------------------------
     *   A method to get the number of enemies in sight
     * 
     *   return: a integer of the number of enemies in sight
     * 
     */
    public int NumbEnemiesSpotted()
    {
        return EnemyAgentsInSight.Count;
    }

    /*
     * Function: NavigateToBase
     * ----------------------------
     *   Creates a navigation pathway to the friendly base
     * 
     */
    public void NavigateToBase()
    {
        NavAgent.TargetCell = GridManager.instance.FindClosestCell(SpawnLocation);
    }

    /*
     * Function: GetColorString
     * ----------------------------
     *   Gets the team colour of the agent. HAS TO READ THE TEXTURE COLOUR NANE, SINCE COLOUR NOT EXPOSED.
     * 
     *   return: a string representing the colour of the team of the agent
     * 
     */
    public string GetColorString()
    {
        GameObject soldierCharacter = gameObject.transform.Find("soldierCharacter").gameObject;
        GameObject armorBody = soldierCharacter.transform.Find("armorBody").gameObject;

        Material material = armorBody.GetComponent<Renderer>().material;

        if (material.name == "soldierBody_Blue (Instance)")
        {
            return "Blue";
        }
        else if (material.name == "soldierBody_Red (Instance)")
        {
            return "Red";
        }
        else
        {
            Debug.Log("It is unknown colour!");
            return "";
        }
    }

    /*
     * Function: IsMyTeamWinning
     * ----------------------------
     *   A method to return a boolean if the agents team is winning or not
     * 
     *   return: a boolean if the agents team is winning or not
     * 
     */
    private bool IsMyTeamWinning()
    {
        bool winning = false;

        if (GetMyTeamScore() > GetEnemyTeamScore())
        {
            winning = true;
        }

        return winning;
    }


    /*
     * Function: GetState
     * ----------------------------
     *   A method to get the current state of the game, which is then transmitted to the server
     * 
     *   return: a string containing the current state of the game
     * 
     */
    private string GetState()
    {
        string state = "";
        //Get current state
        state += EnemyTeamFlagInSight().ToString();
        state += " " + FriendlyTeamFlagInSight().ToString();
        //state += " " + LocationLastIncomingFire.ToString("G4");
        state += " " + IsDamaged.ToString();
        state += " " + HasFlag.ToString();
        state += " " + IsDead.ToString();
        state += " " + FriendlyFlagTaken.ToString();
        state += " " + EnemyFlagTaken.ToString();
        state += " " + NumbEnemiesSpotted();
        state += " " + GetAgentLowHealth().ToString();
        state += " " + LowestHealthEnemyHigherThanAgent().ToString();
        state += " " + TotalEnemyHealthHigherThanAgent().ToString();
        //state += " " + IsMyTeamWinning().ToString();
        return state;
    }

    /*
     * Function: SendReward
     * ----------------------------
     *   Sends the reward to the server
     * 
     *   reward: A integer representing the reward for an action
     * 
     */
    private void SendReward(int reward)
    {
        //Get current state
        string state = GetState();

        prevState = state;
        //Send reward and state
        networkInstance.SendData("REWARD "+ reward.ToString() + " " + state);
    }

    /*
     * Function: SendState
     * ----------------------------
     *   Sends the current state of the game to the server
     * 
     */
    private void SendState()
    {
        //Get current state
        string state = GetState();

        //If state hasnt changed
        if (prevState == state) return;
        prevState = state;
        //Send state
        networkInstance.SendData("STATE " + state);
    }

    /*
     * Function: GetScore
     * ----------------------------
     *   A method that gets the current score of the game
     * 
     *   return: a string of the current score
     * 
     */
    private string GetScore()
    {
        string score = "";
        //Get current score
        
        string red_score = GameObject.Find("A").GetComponent<Text>().text;
        red_score = red_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        string blue_score = GameObject.Find("B").GetComponent<Text>().text;
        blue_score = blue_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        if (teamCol == "Red")
        {
            score += red_score;
            score += " " + blue_score;
        }
        else if (teamCol == "Blue")
        {
            score += blue_score;
            score += " " + red_score;
        }

        return score;
    }

    /*
     * Function: GetMyTeamScore
     * ----------------------------
     *   A method that gets the current score of the team the agent is on
     * 
     *   return: a int of the current score of the team the agent is on
     * 
     */
    private int GetMyTeamScore()
    {
        int teamScore = 0;
        //Get current score

        string red_score = GameObject.Find("A").GetComponent<Text>().text;
        red_score = red_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        string blue_score = GameObject.Find("B").GetComponent<Text>().text;
        blue_score = blue_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        if (teamCol == "Red")
        {
            teamScore = Int32.Parse(red_score);
        }
        else if (teamCol == "Blue")
        {
            teamScore = Int32.Parse(blue_score);
        }

        return teamScore;
    }

    /*
     * Function: GetEnemyTeamScore
     * ----------------------------
     *   A method that gets the current score of the opposing team of the agent
     * 
     *   return: a int of the current score of the enemy team of the agent
     * 
     */
    private int GetEnemyTeamScore()
    {
        int teamScore = 0;
        //Get current score

        string red_score = GameObject.Find("A").GetComponent<Text>().text;
        red_score = red_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        string blue_score = GameObject.Find("B").GetComponent<Text>().text;
        blue_score = blue_score.Split(new[] { "\r\n", "\r", "\n" },
                                    StringSplitOptions.None)[1].Split(' ')[1];

        if (teamCol == "Red")
        {
            teamScore = Int32.Parse(blue_score);
        }
        else if (teamCol == "Blue")
        {
            teamScore = Int32.Parse(red_score);
        }

        return teamScore;
    }
    /*
     * Function: SendScore
     * ----------------------------
     *   Sends the score, if it has changed, to the server
     * 
     */
    private void SendScore()
    {
        //Get current score
        string score = GetScore();

        string state = GetState();

        //If score hasnt changed
        if (prevScore == score) return;

        prevScore = score;
        //Send score
        networkInstance.SendData("SCORE " + score + " " + state);
    }

    /*
     * Function: RotateAround
     * ----------------------------
     *   A method to rotate the agent around
     * 
     */
    private void RotateAround()
    {
        transform.Rotate(0.0f, 90.0f, 0.0f);
    }

    /*
     * Function: LookAtEnemy
     * ----------------------------
     *   A method to have the agent look at an enemy if they see one
     * 
     */
    public void LookAtEnemy()
    {
        if (!EnemiesSpotted())
            return;

        IAgent targetSoldier = EnemyAgentsInSight[0];
        // Any enemy soldiers in sight?
        if (targetSoldier != null)
        {
            LookAt(targetSoldier.GetLocation());
        }
    }

    /*
     * Function: GoToFriendly
     * ----------------------------
     *   A method to navigate to the first enemy in sight
     * 
     */
    public void GoToFriendly()
    {
        if (!FriendlySpotted())
            return;

        IAgent friendlySoldier = FriendlyAgentsInSight[0];
        // Any enemy soldiers in sight?
        if (friendlySoldier != null)
        {
            NavAgent.TargetCell = GridManager.instance.FindClosestCell(friendlySoldier.GetLocation());
        }
    }

    /*
     * Function: ActOnMoveMessage
     * ----------------------------
     *   A method to act on a movement message from the server
     * 
     *   message: A string of the message recieved from the server
     *   repeatedMessage: A boolean saying if this is a reapeated action
     *
     */
    private void ActOnMoveMessage(string message, bool repeatedMessage)
    {
        int afterNodeCount;
        switch (message)
        {
            // Go to enemy base
            case "0":
                if (!repeatedMessage) SetPathToEnemyBase();
                if (!repeatedMessage) prevNodeCount = NavAgent.pathGenerated.Count;
                if (prevNodeCount == 1 || NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                LookAtNextNavPoint();
                MoveToNextNode();
                afterNodeCount = NavAgent.pathGenerated.Count;
                if (afterNodeCount == prevNodeCount - 2 || NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                break;

            // Go to home base
            case "1":
                if (!repeatedMessage) NavigateToBase();
                if (!repeatedMessage) prevNodeCount = NavAgent.pathGenerated.Count;
                if (prevNodeCount == 1)
                {
                    completeMove = true;
                    return;
                }
                if (NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                LookAtNextNavPoint();
                MoveToNextNode();
                afterNodeCount = NavAgent.pathGenerated.Count;
                if (afterNodeCount == prevNodeCount - 2)
                {
                    completeMove = true;
                    return;
                }
                if (NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                break;

            // Go to team mate you can see
            case "2":

                if (!FriendlySpotted())
                {
                    completeMove = true;
                    return;
                }
                if (!repeatedMessage) GoToFriendly();
                if (!repeatedMessage) prevNodeCount = NavAgent.pathGenerated.Count;
                if (prevNodeCount == 1)
                {
                    completeMove = true;
                    return;
                }
                if (NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                LookAtNextNavPoint();
                MoveToNextNode();
                afterNodeCount = NavAgent.pathGenerated.Count;
                if (afterNodeCount == prevNodeCount - 2)
                {
                    completeMove = true;
                    return;
                }
                if (NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                return;

            // Go to random location
            case "3":
                if (!repeatedMessage) SetPathToRandom();
                if (!repeatedMessage) prevNodeCount = NavAgent.pathGenerated.Count;
                if (prevNodeCount == 1)
                {
                    completeMove = true;
                    return;
                }
                if (NavAgent.pathGenerated.Count == 0)
                {
                    completeMove = true;
                    return;
                }
                LookAtNextNavPoint();
                MoveToNextNode();
                afterNodeCount = NavAgent.pathGenerated.Count;
                if (afterNodeCount == prevNodeCount - 2)
                {
                    completeMove = true;
                    return;
                }
                break;

            // Rotate around
            case "4":
                RotateAround();
                moveCounter++;
                if (moveCounter >= 4) completeMove = true;
                return;

            // Look at damage
            case "5":
                LookAtDamage();
                //Reset straight away as its a 1 frame action
                completeMove = true;
                return;
            
            // Look at enemy
            case "6":
                if (!EnemiesSpotted()) { 
                    completeMove = true;
                    return;
                }
                LookAtEnemy();
                //Reset straight away as its a 1 frame action
                completeMove = true;
                return;

            // Look at enemy flag
            case "7":
                if (!EnemyTeamFlagInSight())
                {
                    completeMove = true;
                    return;
                }
                MovetoFlag();
                break;

            // Do nothing
            case "8":
                moveCounter++;
                if (moveCounter >= 50) completeMove = true;
                return;
        }
    }

    /*
     * Function: ActOnActionMessage
     * ----------------------------
     *   A method to act on an action message from the server
     * 
     *   message: A string of the message recieved from the server
     *   repeatedMessage: A boolean saying if this is a reapeated action
     *
     */
    private void ActOnActionMessage(string message, bool repeatedMessage)
    {
        switch (message)
        {
            // Shoot first enemy seen
            case "0":
                int reward = ShootEnemiesInSightWReward();
                if (reward != 0)
                {
                    SendReward(reward);
                }
                if (!EnemiesSpotted()) completeAction = true;
                break;
            
            // Shoot enemy with lowest heatlh
            case "1":
                int reward2 = ShootLowestHealthEnemiesInSightWReward();
                if (reward2 != 0)
                {
                    SendReward(reward2);
                }
                if (!EnemiesSpotted()) completeAction = true;
                break;

            // Pick up flag
            case "2":
                GrabEnemyTeamFlag();
                if (!EnemyTeamFlagInSight()) completeAction = true;
                break;

            // Do nothing
            case "3":
                actionCounter++;
                if (actionCounter >= 10) completeAction = true;
                break;
        }
    }

    /*
     * Function: ActOnMessage
     * ----------------------------
     *   A method that looks at what type of message was recieved from the server and acts upon it
     * 
     *   message: A string of the message recieved from the server
     *   repeatedMessage: A boolean saying if this is a reapeated action (Default: False)
     *
     */
    private void ActOnMessage(string message, bool repeatedMessage = false)
    {
        string[] splitMessage = message.Split(null);
        switch (splitMessage[0])
        {
            case "MOVE_ACTION":
                prevMoveMess = message;
                if (!repeatedMessage)
                {
                    moveCounter = 0;
                    completeMove = false;
                    sentCompleteMove = false;
                }
                ActOnMoveMessage(splitMessage[1], repeatedMessage);
                break;
            case "ACTION_ACTION":
                prevActionMess = message;
                if (!repeatedMessage)
                {
                    actionCounter = 0;
                    completeAction = false;
                    sentCompleteAction = false;
                }
                ActOnActionMessage(splitMessage[1], repeatedMessage);
                break;
        }
    }

    /*
     * Function: Update
     * ----------------------------
     *   The main function that acts on a new message from server,
     *   otherwise it sill it will act upon the previous message recieved.
     *   Then, if the state changes, it will send a new state to the server
     * 
     */
    public void Update()
    {
        string message = networkInstance.CheckForServerUpdate();
        //check if any message from server (updated Q matrix)
        //if so update known Qmatrix
        //Do action based on Qmatrix
        //Debug.Log(message);
        foreach (string mess in message.Split(new[] { "\r\n", "\r", "\n" },
    StringSplitOptions.None))
        {
            

            if (mess != "")
            {
                if (knownIsDead) break;
                completedAction = false;
                ActOnMessage(mess);
            }
            else
            {
                if (knownIsDead) break;
                if (!completeMove) ActOnMessage(prevMoveMess, true);
                if (!completeAction) ActOnMessage(prevActionMess, true);              
            }

        }

        if (completeAction && !sentCompleteAction)
        {
            //send complete action
            networkInstance.SendData("COMPLETE action");
            sentCompleteAction = true;
        }
        
        if (completeMove && !sentCompleteMove)
        {
            //send complete action
            networkInstance.SendData("COMPLETE move");
            sentCompleteMove = true;
        }

        teamCol = teamCol ?? GetColorString();

        if (!move)
        {
            return;
        }

        //Send update if score changes
        SendScore();


        //If now dead but wasnt before then update
        if (!knownIsDead && IsDead && (knownIsDead = true))
        {
            SendReward(deathReward);
        }
        else if (knownIsDead && !IsDead) // was dead, but no longer. Reset known values
        {
            knownIsDead = false;
        }

        //Dont say if see enemy, since dead
        if (knownIsDead) return;

        //Update if thought enemy flag wasnt taken, but actually is
        if (!knownEnemyFlagTaken && EnemyFlagTaken)
        {
            knownEnemyFlagTaken = true;
        }

        //If seen flag for first time since a pickup then give reward
        if (!knownSeenFlag && EnemyTeamFlagInSight())
        {
            knownSeenFlag = true;
            SendReward(seeFlagReward);
        }
        else if ((knownMyTeamScore != GetMyTeamScore()) || (knownEnemyFlagTaken && !EnemyFlagTaken)) //Otherwise see if flag pickup been reset
        {
            knownSeenFlag = false;
        }

        //If just picked up flag then give reward
        if (HasFlag && !knownHasFlag && (knownHasFlag = true))
        {
            SendReward(pickUpFlagReward);
        }else if (!HasFlag && knownHasFlag)// If no longer have flag
        {
            knownHasFlag = false;
        }

        //If damaged but wasnt damaged before then update
        if (IsDamaged && !knownTookDamageRecently && (knownTookDamageRecently = true))
        {
            SendReward(tookDamageReward);
        }
        else if (!IsDamaged && knownTookDamageRecently) //If was damaged and no longer damaged update
        {
            knownTookDamageRecently = false;
        }

        //check if update to known states
        SendState();
    }

    /*
     * Function: Start
     * ----------------------------
     *   Spawns the agent, establishes a connection to the server,
     *   then starts the agent moving
     * 
     */
    public new void Start()
    {
        base.Start();
        networkInstance.ConnectToServer();
        move = true;
    }

    /*
     * Function: OnDestroy
     * ----------------------------
     *   Ends the connection to the server if the agent is destoyed
     * 
     */
    private void OnDestroy()
    {
        networkInstance.OnApplicationQuit();
    }

    /*
     * Function: OnApplicationQuit
     * ----------------------------
     *   Ends the connection to the server if the application is quit
     * 
     */
    void OnApplicationQuit()
    {
        networkInstance.OnApplicationQuit();
    }


}