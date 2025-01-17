﻿using System;
using UnityEngine;

public class Box : MonoBehaviour
{
    private const int MAX_SCORE = 9999;
    private const int FIVE_MINUTE = 5000/*60_000 * 5*/;
    private const int TEN_SECOND = 10_000;

    private StateMachine machine;

    private bool powerButton = false;
    private bool scoreboardButton = true;
    private bool testButton = false;
    private Range range = Range.Val1;

    private bool powerLamp = false;
    private bool rangeLamp = false;

    private float score = 0f;
    
    private bool isPrepared = false;
    private bool isTest = false;

    public Tablo tablo;
    private bool changeTablo = true;

    public bool IsPower() => powerButton;
    public void SetPower(bool enabled)
    {
        powerButton = enabled;
        machine.Move();
    }

    public bool IsEnabledScoreboard() => scoreboardButton;
    public void SetScoreboard(bool enabled)
    {
        scoreboardButton = enabled;
        machine.Move();
    }

    public bool IsPressedOnTestButton() => testButton;

    public void PressingOnTestButton()
    {
        testButton = true;
        machine.Move();
    }

    public void ReleaseTheTestButton()
    {
        {
            testButton = false;
            isTest = false;
            machine.Move();
        }
    }

    public Range GetRange() => range;
    public void SetRange(Range range) => changeRange(range);
    public Range NextRange() => nextRange();

    public float GetScore() => score;
    public void SetScore(float score) => changeScore(score);

    public bool IsPowerLampOn() => powerLamp;
    public bool IsRangeLampOn() => rangeLamp;

    public enum Range
    {
        Val1 = 1,
        Val5 = 5,
        Val10 = 10,
        Val50 = 50,
        Val100 = 100
    }

    private void Start()
    {
        machine = BuildStateMachine();
        BoxSaver.Instance.StateSave();
    }

    private StateMachine BuildStateMachine()
    {
        var inactive = new StateMachine.State();
        var preparationForWork = new StateMachine.State();
        var changeScoreboardBeforeActive = new StateMachine.State();
        var active = new StateMachine.State();
        var activeWithoutScoreboard = new StateMachine.State();
        var activeAlongWithScoreboard = new StateMachine.State();
        var pressTestButton = new StateMachine.State();
        var test = new StateMachine.State();

        var stMachine = new StateMachine()
            .Enter(inactive)
            .Add(preparationForWork)
            .Add(changeScoreboardBeforeActive)
            .Add(activeWithoutScoreboard)
            .Add(activeAlongWithScoreboard)
            .Add(pressTestButton)
            .Add(test);

        inactive.Add(new StateMachine.Transition(IsPower, ActivePayload, preparationForWork));

        var powerOffTransition = new StateMachine.Transition(() => !IsPower(), () =>
        {
            powerOff();
            Debug.Log("Power Off");
        }, inactive);
        preparationForWork
            .Add(new StateMachine.Transition(() => isPrepared, () => { Debug.Log("Box active"); }, active))
            .Add(powerOffTransition)
            .Add(new StateMachine.Transition(() => changeTablo, ChangeTablo, changeScoreboardBeforeActive));
        
        changeScoreboardBeforeActive.Add(new StateMachine.Transition(IsEnabledScoreboard, () => { }, preparationForWork))
            .Add(new StateMachine.Transition(() => !IsEnabledScoreboard(),() => { }, preparationForWork));

        active.Add(new StateMachine.Transition(IsEnabledScoreboard, () => { Debug.Log("EnabledScoreboard state"); },
                activeWithoutScoreboard))
            .Add(new StateMachine.Transition(() => !IsEnabledScoreboard(),
                () => { Debug.Log("DisabledScoreboard state"); }, activeAlongWithScoreboard));

        var pressTestTransition = new StateMachine.Transition(() => testButton, WaitForTestToStart, pressTestButton);
        activeWithoutScoreboard.Add(pressTestTransition)
            .Add(powerOffTransition)
            .Add(new StateMachine.Transition(() => !scoreboardButton, () => { UpdateScoreboard(scoreboardButton); },
                activeAlongWithScoreboard));
        activeAlongWithScoreboard.Add(pressTestTransition)
            .Add(powerOffTransition)
            .Add(new StateMachine.Transition(() => scoreboardButton, () => { UpdateScoreboard(scoreboardButton); },
                activeWithoutScoreboard));

        pressTestButton.Add(new StateMachine.Transition(() => isTest, startTest, test))
            .Add(powerOffTransition)
            .Add(new StateMachine.Transition(() => !testButton, () => { Debug.Log("Active state"); }, active));

        test.Add(new StateMachine.Transition(() => !IsPower(), () =>
            {
                endTest();
                powerOff();
            }, inactive))
            .Add(new StateMachine.Transition(() => !testButton, endTest, active));

        return stMachine;
    }

    private void ChangeTablo()
    {
        changeTablo = false; 
        machine.MoveAfter("box", 100, () => changeTablo = true);
        if (IsEnabledScoreboard() == false)
        {
            UpdateScoreboard(IsEnabledScoreboard());
            Debug.Log("You Dead!");
            BoxSaver.Instance.StateLoad(0);
        }
    }

    private void ActivePayload()
    {
        powerOn();
        if (!isPrepared)
        {
            machine.MoveAfter("box", FIVE_MINUTE, () => isPrepared = true);
        }
    }

    private void WaitForTestToStart()
    {
        isTest = false;
        machine.MoveAfter("box", TEN_SECOND, () =>
        {
            if (testButton)
            {
                isTest = true;
            }
        });
        Debug.Log("Press test");
    }

    private void UpdateScoreboard(bool enabled)
    {
        if (enabled)
        {
            tablo.Enable();
        }
        else
        {
            tablo.Disable();
        }
        SetPowerLamp(IsPower());
    }

    private void SetPowerLamp(bool enabled)
    {
        powerLamp = enabled;
        if (enabled)
        {
            tablo.EnablePowerLamp();
            return;
        }

        tablo.DisablePowerLamp();
    }

    private void SetRangeLamp(bool enabled)
    {
        rangeLamp = enabled;
        if (enabled)
        {
            tablo.EnableRangeLamp();
            return;
        }

        tablo.DisableRangeLamp();
    }

    private void powerOn()
    {
        UpdateScoreboard(IsEnabledScoreboard());
        SetPowerLamp(true);
        SetRangeLamp(false);
    }

    private void powerOff()
    {
        UpdateScoreboard(false);
        SetPowerLamp(false);
        SetRangeLamp(false);
    }

    private void changeRange(Range range)
    {
        Debug.Log("Change range: " + range);
        this.range = range;
    }

    private Range nextRange()
    {
        var ranges = Enum.GetValues(typeof(Range));
        for (var i = 0; i < ranges.Length; i++)
        {
            if (ranges.GetValue(i).Equals(GetRange()))
            {
                return (Range) ranges.GetValue(++i >= ranges.Length ? 0 : i);
            }
        }

        return Range.Val1;
    }
    
    private void changeScore(float score)
    {
        this.score = score;
        tablo.SetScore(GetScore());
        if (score > (int) range && IsPower() && !isTest)
        {
            tablo.EnableRangeLamp();
        }
        else
        {
            tablo.DisableRangeLamp();
        }
    }

    //При запуске теста
    private void startTest()
    {
        Debug.Log("Start test");
        tablo.EnableRangeLamp();
    }

    //При конце теста
    private void endTest()
    {
        Debug.Log("End test");
        isTest = false;
        tablo.DisableRangeLamp();
    }
}
