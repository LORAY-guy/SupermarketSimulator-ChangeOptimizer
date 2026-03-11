using System;

using HarmonyLib;

namespace ChangeOptimizer;

[HarmonyPatch(typeof(Checkout), "Start")]
class CheckoutStartPatch
{
    static void Postfix(Checkout __instance) =>
        RegisterScreenManager.SetupScreen(__instance);
}

[HarmonyPatch(typeof(Checkout), "ClearCheckout")]
class ClearCheckoutPatch
{
    static void Postfix(Checkout __instance) => RegisterScreenManager.ResetChange(__instance);
}

[HarmonyPatch(typeof(Checkout), "ResetCashRegister")]
class ResetCashRegisterPatch
{
    static void Postfix(Checkout __instance) => RegisterScreenManager.ResetChange(__instance);
}

[HarmonyPatch(typeof(Checkout), "TookCustomersCash")]
class TookCashPatch
{
    static void Prefix(Checkout __instance, float payment)
    {
        int changeCents = (int)Math.Round((payment - __instance.TotalPrice) * 100f, MidpointRounding.AwayFromZero);
        RegisterScreenManager.ShowChange(__instance, changeCents);
    }
}

[HarmonyPatch(typeof(Checkout), "AddOrRemoveChange")]
class CollectedChangePatch
{
    static void Postfix(Checkout __instance, MoneyPack money, bool add)
    {
        int denomCents = (int)Math.Round(money.Value * 100f, MidpointRounding.AwayFromZero);
        RegisterScreenManager.TrackCoin(__instance, denomCents, add);
    }
}
