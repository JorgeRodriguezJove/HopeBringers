﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValkyrieData : CharacterData
{

    public override void UpdateMyUnitStatsForTheLevel()
    {
        //Referencia al personaje en el nivel
        myUnitReferenceOnLevel = FindObjectOfType<Valkyrie>();

        if (myUnitReferenceOnLevel != null)
        {
            //Aztualizo las mejoras genéricas
            base.UpdateMyUnitStatsForTheLevel();

            //Actualizo las merjoas especificas del personaje
            //HAY QUE CAMBIARLO EN SU SCRIPT
            myUnitReferenceOnLevel.GetComponent<Valkyrie>().SetSpecificStats(specificIntCharacterUpgrades[AppValkyrieUpgrades.moreRange1], specificBoolCharacterUpgrades[AppValkyrieUpgrades.moreRange2],
                                                                             specificBoolCharacterUpgrades[AppValkyrieUpgrades.armorChange1], specificBoolCharacterUpgrades[AppValkyrieUpgrades.armorChange2],
                                                                             specificBoolCharacterUpgrades[AppValkyrieUpgrades.sustitution1], specificBoolCharacterUpgrades[AppValkyrieUpgrades.sustitution2],
                                                                             specificIntCharacterUpgrades[AppValkyrieUpgrades.height1], specificIntCharacterUpgrades[AppValkyrieUpgrades.height2]);
        }
    }

    //Esto se llama en el INIT del characterData (padre de este script)
    protected override void InitializeSpecificUpgrades()
    {
        //Mejoras Tipo BOOL
        //HAY QUE CAMBIARLO EN SU SCRIPT
        specificIntCharacterUpgrades.Add(AppValkyrieUpgrades.moreRange1, 0);
        specificBoolCharacterUpgrades.Add(AppValkyrieUpgrades.moreRange2, false);

        specificBoolCharacterUpgrades.Add(AppValkyrieUpgrades.armorChange1, false);
        specificBoolCharacterUpgrades.Add(AppValkyrieUpgrades.armorChange2, false);

        specificBoolCharacterUpgrades.Add(AppValkyrieUpgrades.sustitution1, false);
        specificBoolCharacterUpgrades.Add(AppValkyrieUpgrades.sustitution2, false);

        specificIntCharacterUpgrades.Add(AppValkyrieUpgrades.height1, 0);
        specificIntCharacterUpgrades.Add(AppValkyrieUpgrades.height2, 0);

        //DESCRIPCIONES DE LAS MEJORAS. Estos no hace falta inicializarlos en la unidad ingame. Están aqui simplemente para los árboles de mejoras
        //Activas
        specificStringCharacterUpgrades.Add("moreRange1Text", AppValkyrieUpgrades.moreRange1Text);
        specificStringCharacterUpgrades.Add("moreRange2Text", AppValkyrieUpgrades.moreRange2Text);

        specificStringCharacterUpgrades.Add("armorChange1Text", AppValkyrieUpgrades.armorChange1Text);
        specificStringCharacterUpgrades.Add("armorChange2Text", AppValkyrieUpgrades.armorChange2Text);

        //Pasivas
        specificStringCharacterUpgrades.Add("sustitution1Text", AppValkyrieUpgrades.sustitution1Text);
        specificStringCharacterUpgrades.Add("sustitution2Text", AppValkyrieUpgrades.sustitution2Text);

        specificStringCharacterUpgrades.Add("height1Text", AppValkyrieUpgrades.height1Text);
        specificStringCharacterUpgrades.Add("height2Text", AppValkyrieUpgrades.height2Text);
    }

}

