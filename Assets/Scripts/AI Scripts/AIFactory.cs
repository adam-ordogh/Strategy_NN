// AIFactory.cs
using System;
using System.Collections.Generic;
using Unity.InferenceEngine;

public static class AIFactory
{
    public enum AIType
    {
        Deterministic,
        MLBasic,
        MLAggressive,
        MLDefensive,
        MLEconomic
    }

    public static IAIController CreateAI(AIType type, int playerId, bool isTraining = false, ModelAsset model = null)
    {
        switch (type)
        {
            case AIType.Deterministic:
                return new AIMacroDeterministic(playerId);

            case AIType.MLBasic:
                // Pass isTraining to the controller
                return new AIMacroMLController(playerId, isTraining, model);

            default:
                throw new ArgumentException($"Unknown AI type: {type}");
        }
    }
}