// AIFactory.cs
using System;
using System.Collections.Generic;

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

    public static IAIController CreateAI(AIType type, int playerId)
    {
        switch (type)
        {
            case AIType.Deterministic:
                return new AIMacroDeterministic(playerId);

            case AIType.MLBasic:
                var mlBasic = new AIMacroMLController(playerId);
                // You could configure the ML agent here (e.g., different model files)
                return mlBasic;

            //case AIType.MLAggressive:
            //    var mlAgg = new AIMacroMLController(playerId);
            //    // Load aggressive model
            //    return mlAgg;

            //case AIType.MLDefensive:
            //    var mlDef = new AIMacroMLController(playerId);
            //    // Load defensive model
            //    return mlDef;

            //case AIType.MLEconomic:
            //    var mlEco = new AIMacroMLController(playerId);
            //    // Load economic model
            //    return mlEco;

            default:
                throw new ArgumentException($"Unknown AI type: {type}");
        }
    }
}