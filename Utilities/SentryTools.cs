using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wired.Utilities;

public static class SentryTools
{
    public static Action<InteractableSentry, Player> setTargetPlayer;
    public static Action<InteractableSentry, Animal> setTargetAnimal;
    public static Action<InteractableSentry, InteractableVehicle> setTargetVehicle;
    public static Action<InteractableSentry, Zombie> setTargetZombie;

    static SentryTools()
    {
        CompileAction("targetPlayer", ref setTargetPlayer);
        CompileAction("targetZombie", ref setTargetZombie);
        CompileAction("targetAnimal", ref setTargetAnimal);
        CompileAction("targetVehicle", ref setTargetVehicle);
    }

    static void CompileAction<T>(string fieldName, ref Action<InteractableSentry,T> target)
    {
        FieldInfo fieldInfo = typeof(InteractableSentry).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        ParameterExpression instanceParam = Expression.Parameter(typeof(InteractableSentry), "instance");
        ParameterExpression valueParam = Expression.Parameter(typeof(T), "value");

        MemberExpression fieldAccess = Expression.Field(instanceParam, fieldInfo);

        BinaryExpression assignment = Expression.Assign(fieldAccess, valueParam);

        target = Expression.Lambda<Action<InteractableSentry, T>>(
            assignment,
            instanceParam,
            valueParam
        ).Compile();
    }

}
