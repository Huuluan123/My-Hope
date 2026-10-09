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
Được rồi, mình sẽ gặp bạn sau nhé!
#speaker: Hope
Được thôi, hẹn gặp lại!
-> END

== AfterBeingSaved ==
# speaker: Anna
Cản ơn bạn đã cứu mình!
# speaker: Hope
Nhiệm vụ của mình là cứu và yêu thương con người. Không có gì to tác đâu!
# speaker: Anna
Bạn trông tốt bụng và còn là một robot nữa. Bạn đến từ đâu thế?
# speaker: Hope
Mình không biết nữa. Ký ức của mình trống trơn!
# speaker: Anna
Vậy thì tên bạn là gì vậy?
# speaker: Hope
Mình là Robot. Nhiệm vụ của mình là cứu và yêu thương con người. Đó là tất cả những gì trong ký ức của mình.
# speaker: Anna
Mình là Anna. Rất vui được gặp bạn, Robot!
Có một ngôi làng không xa đây. Đó là nơi mình sống. Có rất nhiều người ở đó. Bạn có muốn đến cùng mình không?
# speaker: Hope
Được thôi!
# speaker: System
Mở khoá khu vực mới: Làng.
-> END

== Anna_Village ===
#speaker: Anna
Chào mừng đến với ngôi làng của mình! Mình nghĩ bạn sẽ thích nó đấy!
Tại sao không thử dạo một vòng quanh ngôi làng nhỉ? Nó sẽ khá vui đấy!
-> END

=== Anna_Village_HardChallengeCompleted ===
~SetState("Anna_HardChallenge_Talked", true)
#speaker: Anna
Chúc mừng, bạn đã hoàn thành thử thách khó! Bạn là một robot tuyệt vời!
#speaker: Hope
Không có gì đâu, bạn tôi!
-> END

=== SceamForHelp ===
# speaker: Anna
CỨU TÔI VỚI!
# speaker: System
CẢNH BÁO: Phát hiện con người đang gặp nguy hiểm!
KÍCH HOẠT NHIỆM VỤ GIẢI CỨU!
->END