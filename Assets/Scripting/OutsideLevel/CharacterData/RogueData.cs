﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RogueData : CharacterData
{

    public override void UpdateMyUnitStatsForTheLevel()
    {
        //Referencia al personaje en el nivel
        myUnitReferenceOnLevel = FindObjectOfType<Rogue>();

        if (myUnitReferenceOnLevel != null)
        {
            //Aztualizo las mejoras genéricas
            base.UpdateMyUnitStatsForTheLevel();

            //Inicializo las variables especificas del personaje
            myUnitReferenceOnLevel.GetComponent<Rogue>().SetSpecificStats(specificBoolCharacterUpgrades[AppRogueUpgrades.multiJumpAttack1], specificIntCharacterUpgrades[AppRogueUpgrades.multiJumpAttack2],
                                                                          specificBoolCharacterUpgrades[AppRogueUpgrades.extraTurnAfterKill1], specificIntCharacterUpgrades[AppRogueUpgrades.extraTurnAfterKill2],
                                                                          specificBoolCharacterUpgrades[AppRogueUpgrades.smokeBomb1], specificBoolCharacterUpgrades[AppRogueUpgrades.smokeBomb2],
                                                                          specificBoolCharacterUpgrades[AppRogueUpgrades.buffDamageKill1], specificBoolCharacterUpgrades[AppRogueUpgrades.buffDamageKill2]);
        }
    }

    //Esto se llama en el INIT del characterData (padre de este script)
    protected override void InitializeSpecificUpgrades()
    {
        //Mejoras Tipo BOOL
        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.multiJumpAttack1, false);
        specificIntCharacterUpgrades.Add(AppRogueUpgrades.multiJumpAttack2, 0);

        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.extraTurnAfterKill1, false);
        specificIntCharacterUpgrades.Add(AppRogueUpgrades.extraTurnAfterKill2, 0);

        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.smokeBomb1, false);
        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.smokeBomb2, false);

        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.buffDamageKill1, false);
        specificBoolCharacterUpgrades.Add(AppRogueUpgrades.buffDamageKill2, false);
    }



























    //CODIGO ANTIGUO COMPROBAR SI SE PUEDE BORRAR
    //RogueData otherRogueInScene;



    //public override void Awake()
    //{
    //    otherRogueInScene = FindObjectOfType<RogueData>();

    //    if (otherRogueInScene != null && otherRogueInScene.gameObject != this.gameObject)
    //    {
    //        Destroy(otherRogueInScene.gameObject);
    //    }

    //    base.Awake();
    //}
}
