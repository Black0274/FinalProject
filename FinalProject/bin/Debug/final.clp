(deftemplate ioproxy  
	(slot fact-id)       
	(multislot answers)   
	(multislot messages)   
	(slot value)          
)


(deffacts proxy-fact
	(ioproxy
		(fact-id 0112)
		(value none)   
		(messages)    
	)
)

(defrule clear-messages
	(declare (salience 90))
	?clear-msg-flg <- (clearmessage)
	?proxy <- (ioproxy)
	=>
	(modify ?proxy (messages))
	(retract ?clear-msg-flg)
	(printout t "Messages cleared ..." crlf)	
)

(defrule set-output-and-halt
	(declare (salience 99))
	?current-message <- (sendmessagehalt ?new-msg)
	?proxy <- (ioproxy (messages $?msg-list))
	=>
	(printout t "Message set : " ?new-msg " ... halting ..." crlf)
	(modify ?proxy (messages ?new-msg))
	(retract ?current-message)
	(halt)
)

(defrule append-output-and-halt
	(declare (salience 99))
	?current-message <- (appendmessagehalt $?new-msg)
	?proxy <- (ioproxy (messages $?msg-list))
	=>
	(printout t "Messages appended : " $?new-msg " ... halting ..." crlf)
	(modify ?proxy (messages $?msg-list $?new-msg))
	(retract ?current-message)
	(halt)
)

(defrule set-output-and-proceed
	(declare (salience 99))
	?current-message <- (sendmessage ?new-msg)
	?proxy <- (ioproxy)
	=>
	(printout t "Message set : " ?new-msg " ... proceed ..." crlf)
	(modify ?proxy (messages ?new-msg))
	(retract ?current-message)
)

(defrule append-output-and-proceed
	(declare (salience 99))
	?current-message <- (appendmessage ?new-msg)
	?proxy <- (ioproxy (messages $?msg-list))
	=>
	(printout t "Message appended : " ?new-msg " ... proceed ..." crlf)
	(modify ?proxy (messages $?msg-list ?new-msg))
	(retract ?current-message)
)

(defrule print-messages
	(declare (salience 99))
	?proxy <- (ioproxy (messages ?msg-list))
	?update-key <- (updated True)
	=>
	(retract ?update-key)
	(printout t "Messages received : " ?msg-list crlf)
)

;======================================================================================
(deftemplate planet 
	(slot plan)
)

(defrule Луна
	(declare (salience 35))
	?planeta <- (planet (plan Луна))
	=>
	(assert (appendmessagehalt (str-cat "Скорее всего, из-за влияния Луны вы склонны к постоянным эмоциональным переживаниям и волнениям.")))
)

(defrule Меркурий
	(declare (salience 35))
	?planeta <- (planet (plan Меркурий))
	=>
	(assert (appendmessagehalt (str-cat "Меркурий плохо на вас влияет, поэтому вы сегодня суетливее, чем обычно.")))
)

(defrule Венера
	(declare (salience 35))
	?planeta <- (planet (plan Венера))
	=>
	(assert (appendmessagehalt (str-cat "Венера заставляет вас скапливать свой гнев внутри, слава богу.")))
)

(defrule Земля
	(declare (salience 35))
	?planeta <- (planet (plan Земля))
	=>
	(assert (appendmessagehalt (str-cat "Земля оказывает сегодня на вас серьезное влияние, так же как и вчера, и завтра тоже.")))
)

(defrule Марс
	(declare (salience 35))
	?planeta <- (planet (plan Марс))
	=>
	(assert (appendmessagehalt (str-cat "Если вы чувствуете сранные силы, исходящие с Марса, это явный признак того, что вым прямой путь на РЭН-ТВ.")))
)

(defrule Юпитер
	(declare (salience 35))
	?planeta <- (planet (plan Юпитер))
	=>
	(assert (appendmessagehalt (str-cat "По сравнению с Юпитером вы слишком малы и незначительны, поэтому он вас не замечает и никак не влияет.")))
)

(defrule Сатурн
	(declare (salience 35))
	?planeta <- (planet (plan Сатурн))
	=>
	(assert (appendmessagehalt (str-cat "Сатурн печется за вас и советует заняться спортом. Например, покрутить хулахуп.")))
)

(defrule Уран
	(declare (salience 35))
	?planeta <- (planet (plan Уран))
	=>
	(assert (appendmessagehalt (str-cat "Уран слишком нестабилен, но некоторые сценаристы супергероики считают, что его воздействие это неплохая идея.")))
)

(defrule Нептун
	(declare (salience 35))
	?planeta <- (planet (plan Нептун))
	=>
	(assert (appendmessagehalt (str-cat "Чтобы добиться расположения Нептуна, вам придется постараться растопить его ледяные недра.")))
)

(defrule Плутон
	(declare (salience 35))
	?planeta <- (planet (plan Плутон))
	=>
	(assert (appendmessagehalt (str-cat "Плутон слишком обижен приписываемой ему незначительностью, поэтому не хочет согодня на вас влиять.")))
)

(defrule Exit 
	(declare (salience 10))
	=> 
	(assert (sendmessagehalt "Больше ничего не видно."))
) 