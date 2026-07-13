INCLUDE ../Global/Global.ink

~ temp Anna_RescueScene = CheckState("Anna_RescueScene")
~ temp Anna_HardChallenge_Talked = CheckState("Anna_HardChallenge_Talked")

->Start

=== Start ===
{
    - Anna_RescueScene and npc_location == "Forest":
        -> AfterBeingSaved
        
    - npc_location == "Village" and not CheckState("Village_HardChallengeCompleted"):
        -> Anna_Village

    - npc_location == "Village" and CheckState("Village_HardChallengeCompleted") and not Anna_HardChallenge_Talked:
        -> Anna_Village_HardChallengeCompleted
        
    - else:
        -> Anna_Default
}

=== Anna_Default ===
#speaker: Anna
Okay, I'll catch you later then!
#speaker: Hope
Sure, see you later!
-> END

=== AfterBeingSaved ===
# speaker: Anna
Thank you for saving me!
# speaker: Hope
My mission is to save and love humans. You are welcome!
# speaker: Anna
You seem nice and you are a robot, I see. Where do you come from?
# speaker: Hope
I have no idea. My memory is blank.
# speaker: Anna
What is your name by the way?
# speaker: Hope
I'm Robot. My mission is to save and love humans. That's all what in my memory.
# speaker: Anna
I'm Anna. Nice to meet you, Robot!
There's a village not far from here. That's where I live. There are plenty of people there. Do you want to come with me?
# speaker: Hope
Sure, i guess!
# speaker: System
New location unlocked: Village.
-> END

=== Anna_Village ===
#speaker: Anna
Welcome to my village! I hope you will like it here.
Why don't you go and explore the village? It might be fun.
-> END

=== Anna_Village_HardChallengeCompleted ===
~SetState("Anna_HardChallenge_Talked", true)
#speaker: Anna
Congrats, you have completed the hard challenge! You are a great robot!
#speaker: Hope
You're welcome, my friend!
-> END

=== SceamForHelp ===
# speaker: Anna
HELP!
# speaker: System
Warning: Human in danger detected!
Rescue mission initiated!
->END