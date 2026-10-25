INCLUDE ../Global/Global.ink

-> START

=== START ===
// Di chuyển khai báo vào bên trong Knot
~ temp Frog_InvitedToHouse = CheckState("Frog_InvitedToHouse")

{
    - Frog_InvitedToHouse:
        -> Frog_Default
    - else:
        -> Invite_to_house
}

=== Invite_to_house ===
#speaker: Frog
Yo, What's up. Can a loc come up in your crib?

* [Man, fuck you! I'll see you at work.]
    Oh, Nibba don't hate me cause I'm beautiful, Nibba.
    Maybe if you get rid that yee-yee-ass haircut, you'll get some peaches on your dih.
    ~ SetState("Frog_GotAcceptedToHouse", false)
    -> After_invite
* [Sure, come on in.]
    Okay, thank you man.
    ~ SetState("Frog_GotAcceptedToHouse", true)
    -> After_invite

=== After_invite ===
Nibba.
~SetState("Frog_InvitedToHouse", true)
-> END

=== Frog_Default ===
~ temp accept = CheckState("Frog_GotAcceptedToHouse")

{ accept:
    #speaker: Frog
    Thank you for letting me in, man. I appreciate it. I got a little something for you, man. Check it out.
- else:
    #speaker: Frog
    Oh, someone didn't let me in his crib. Ain't cool, bro.
}
-> END