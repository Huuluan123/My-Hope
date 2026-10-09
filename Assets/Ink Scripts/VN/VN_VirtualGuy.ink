INCLUDE ../Global/Global.ink

~ temp has_met = CheckState("VirtualGuy_Met")

-> virtual_guy_meet

=== virtual_guy_meet ===
{ has_met == false:
    #speaker: VirtualGuy
    Ồ! Bạn đã tìm thấy tôi! Có vẻ như bạn đã hoàn thành những thử thách khó rồi! Chúc mừng nha!
    Rất vui được gặp bạn đấy, bạn của tôi!
    
    ~ SetState("VirtualGuy_Met", true)
    -> END
- else:
    #speaker: VirtualGuy
    Ừ thì, đến lúc phải tiếp tục việc của mình rồi bạn à!
    -> END
}