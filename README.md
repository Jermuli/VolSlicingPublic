# Volumetrisen datan viipaloinnin WPF käyttöliittymä

## Kontrollit

Hiiren vasen painike pohjassa hiirellä raahaaminen vaihtaa tarkastelukulmaa

Hiiren oikea painike pohjassa hiirellä raahaminen pannaa kuvaa

Hiiren rulla vaihtaa poikkileikkauksen syvyyttä yhden yksikön kerrallaan

Hiiren rulla + vasen ctrl zoomaa ja loitontaa näkymää

Hiiren rulla + shift vaihtaa poikkileikkauksen syvyyttä 10 yksikön verran

Z resetoi pannauksen takaisin keskipisteeseen

X siirtää poikkileikkauksen keskelle (0)

## Kääntäminen

Visual studio projektia pitäisi pystyä ajamaan suoraan visual studiolla, 
olettaen että .NET 6 kehitysympäristö on asennettu. Ensimmäisellä projektin 
avaamiskerralla 
voi kestää hetki ennen kuin käytetyt kirjastot saadaan ladattua nugetista. 

## Yleisimmät kääntämisen ongelmat 

Jos kääntämisen yhteydessä ohjelma kaatuu ja heittää 
viestiä puuttuvista shadereista/tekstuureista, kopio kansiot Textures ja 
Shaders ajettavan ohjelman kanssa samaan kansioon.

Välillä jos ohjelmaa yrittää kääntää ja käynnistää useita kertoja pienen ajan sisällä, 
ohjelma saattaa kaatua ja antaa virheviestin, jossa kerrotaan että 
jokin resurssi on jo muun prosessin käytössä. Johtuu luultavasti siitä, että 
aiempi ohjelman instanssi vielä jäänyt kummittelemaan taustalle. Odota hetki ja 
kokeile uudestaan.
