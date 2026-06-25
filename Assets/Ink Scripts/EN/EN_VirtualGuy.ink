INCLUDE ../Global/Global.ink

~ temp has_met = CheckState("VirtualGuy_Met")

-> virtual_guy_meet

=== virtual_guy_meet ===
{ has_met == false:
    #speaker: VirtualGuy
    Oh! You found me! Seems like you have completed hard challenges. Congrats!
    It's really nice to meet you, pal!
    
    ~ SetState("VirtualGuy_Met", true)
    -> END
- else:
    #speaker: VirtualGuy
    Well, we should be getting on!
    -> END
}