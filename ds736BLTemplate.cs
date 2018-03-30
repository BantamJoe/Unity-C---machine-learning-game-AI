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

    private int lowHealth = 30;

    private int killEnemyReward = 2;
    private int pickUpFlagReward = 3;
    private int deathReward = -5;
    private int tookDamageReward = -1;


    public void MovetoFlag()
    {
        if (!EnemyTeamFlagInSight()) return;
        LookAt(EnemyFlagInSight.GetLocation());
        MoveTowards(EnemyFlagInSight.GetLocation());
    }

    public bool FriendlyTeamFlagInSight()
    {
        return FriendlyFlagInSight != null;
    }

    public bool FriendlySpotted()
    {
        return FriendlyAgentsInSight.Count > 0;
    }

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
                    return 2;
                }
            }
        }
        return 0;
    }

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
                return 2;
            }
        }

        return 0;
    }

    public int GetAgentHealth()
    {
        return Health;
    }

    public bool GetAgentLowHealth()
    {
        if (GetAgentHealth() <= lowHealth) return true;
        return false;
    }

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

    public bool TotalEnemyHealthHigherThanAgent()
    {
        if (GetEnemyHealthInSight() > GetAgentHealth()) return true;
        return false;
    }

    public bool LowestHealthEnemyHigherThanAgent()
    {
        if (GetLowestHealthEnemy() > GetAgentHealth()) return true;
        return false;
    }

    public int NumbEnemiesSpotted()
    {
        return EnemyAgentsInSight.Count;
    }

    public void NavigateToBase()
    {
        NavAgent.TargetCell = GridManager.instance.FindClosestCell(SpawnLocation);
    }

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
        return state;
    }

    private void SendReward(int reward)
    {
        //Get current state
        string state = GetState();

        prevState = state;

        //Send reward and state
        networkInstance.SendData("REWARD "+ reward.ToString() + " " + state);
    }

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

    private void RotateAround()
    {
        transform.Rotate(0.0f, 90.0f, 0.0f);
    }

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
                //if (NavAgent.pathGenerated.Count == 0) ResetParameters();
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

    private void ResetParameters(int actionType = 0)
    {
        
        if (actionType == 1)
        {//If action action
            completeAction = true;
        }
        else if (actionType == 2)
        {// If move action
            completeMove = true;
        }
        if (completeAction && completeMove) { 
            completedAction = true;
            moveCounter = 0;
            actionCounter = 0;
            completeAction = false;
            completeMove = false;
        }
    }

    public void Update()
    {
        string message = networkInstance.checkForServerUpdate();
        //check if any message from server (updated Q matrix)
        //if so update known Qmatrix
        //Do action based on Qmatrix
        //Debug.Log(message);
        foreach (string mess in message.Split(new[] { "\r\n", "\r", "\n" },
    StringSplitOptions.None))
        {
            

            if (mess != "")
            {
                ResetParameters(1);
                ResetParameters(2);
                if (knownIsDead) break;
                completedAction = false;
                ActOnMessage(mess);
                //Debug.Log(mess);

                //prevMess = mess;
                //Debug.Log(mess);
                //Debug.Log(mess.Length);
            }
            else
            {
                if (knownIsDead) break;
                if (!completeMove) ActOnMessage(prevMoveMess, true);
                if (!completeAction) ActOnMessage(prevActionMess, true);
                /*
                if (!completedAction)
                {
                    //Debug.Log(prevMoveMess);
                    //Dont act as you are dead
                    if (knownIsDead) break;
                    //Debug.Log("prev move message");
                    //Debug.Log(prevMoveMess);
                    ActOnMessage(prevMoveMess, true);
                    ActOnMessage(prevActionMess, true);
                }
                else
                {
                    prevState = "";
                    completedAction = false;
                }*/                
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

        //Vector3 vector3 = new Vector3(10.0f, 20.0f, 30.0f);
        //Move(vector3);
    }

    public new void Start()
    {
        base.Start();

        networkInstance.ConnectToServer();
        move = true;
    }

    private void OnDestroy()
    {
        networkInstance.OnApplicationQuit();
    }

    void OnApplicationQuit()
    {
        networkInstance.OnApplicationQuit();
    }


}