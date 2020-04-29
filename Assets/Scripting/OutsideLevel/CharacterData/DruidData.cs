﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DruidData : CharacterData
{

    public override void UpdateMyUnitStatsForTheLevel()
    {
        //Referencia al personaje en el nivel
        myUnitReferenceOnLevel = FindObjectOfType<Druid>();

        if (myUnitReferenceOnLevel != null)
        {
            //Aztualizo las mejoras genéricas
            base.UpdateMyUnitStatsForTheLevel();

            //Actualizo las merjoas especificas del personaje
            //HAY QUE CAMBIARLO EN SU SCRIPT
            myUnitReferenceOnLevel.GetComponent<Druid>().SetSpecificStats(specificIntCharacterUpgrades[AppDruidUpgrades.heal1], specificBoolCharacterUpgrades[AppDruidUpgrades.heal2],
                                                                          specificBoolCharacterUpgrades[AppDruidUpgrades.areaHeal1], specificBoolCharacterUpgrades[AppDruidUpgrades.areaHeal2],
                                                                          specificBoolCharacterUpgrades[AppDruidUpgrades.tile1], specificBoolCharacterUpgrades[AppDruidUpgrades.tile2],
                                                                          specificBoolCharacterUpgrades[AppDruidUpgrades.tileMovement1], specificBoolCharacterUpgrades[AppDruidUpgrades.tileMovement2]);
        }
    }

    //Esto se llama en el INIT del characterData (padre de este script)
    protected override void InitializeSpecificUpgrades()
    {
        //Mejoras Tipo BOOL
        //HAY QUE CAMBIARLO EN SU SCRIPT
        specificIntCharacterUpgrades.Add(AppDruidUpgrades.heal1, 0);
        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.heal2, false);

        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.areaHeal1, false);
        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.areaHeal2, false);

        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.tile1, false);
        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.tile2, false);

        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.tileMovement1, false);
        specificBoolCharacterUpgrades.Add(AppDruidUpgrades.tileMovement2, false);

       //Texts
    }

}

