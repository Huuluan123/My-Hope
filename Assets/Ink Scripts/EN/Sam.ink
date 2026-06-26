INCLUDE ../Global/Global.ink

~ temp Anna_RescueScene = CheckState("Anna_RescueScene")

->Start

=== Start ===
{ Anna_RescueScene:
    -> AfterBeingSaved
- else:
    -> Anna_Town
}

== AfterBeingSaved ==
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
There's a town not far from here. That's where I live. There are plenty of people there. Do you want to come with me?
# speaker: Hope
Sure, i guess!
# speaker: System
New location unlocked: [town] Town.
-> END

== Anna_Town ===
#speaker: Anna
Welcome to my town! I hope you will like it here.
Why don't you go and explore the town? It might be fun.
-> END