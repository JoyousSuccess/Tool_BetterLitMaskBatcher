
is gotta be in Editor/  folder  cause its editor tool thingy   ( it is .. if u move it and it dont work, thats probably why )



0. -- Notice --
________________________________

Tool to batch make stuff with Jason's Better Lit shader version 2021
maybe it works for 2020 or other versions.. i dunno man.. i was using 2021 mkay.


this is kinda hacky and I made it fast for my personal use
maybe someone can find this useful.

it is better than doing each texture set manually.. maybe..


no support is given beyond this readme,
 	you are an engineer and scientist now figure it out my friend.
		Be like Newton! be like Tesla!  
		... hopefully you wont die a virgin ... I might lolol... Sadge

Rules and Laws are for closed minded people this is free software, God made it, its a product of Nature.



== == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == 

1. -- Get to the Tool --
________________________________



found at : 
Window  >  Better Lit Shader  >  Batch Pack Mask Mat



== == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == 

2. -- How to Use --
________________________________


=== YOUR FILES TO BETTER LIT MASK FILE ===		== == == == == == == == == == == == == == ==

MAKE FOLDER NAMED "MaskBl" inside the Inputs files folder... the folder with your files you gonna input in this thing and batch...



the top lists are to make Better Lit Mask files from your files for :

Metallic
AmbientOcclusion
Detail
Smoothness

tick the bool for "All Smooth is Roughness Maps"  if all your files are roughness and it will invert it to become smoothness maps

then press Do Mask Pack


Serious things to note:

	MAKE FOLDER NAMED "MaskBl" inside the Inputs files folder... the folder with your files you gonna input in this thing and batch...

	Also name _r or _s for rough / smooth ... line 313 uses it to auto name your stuff or something i forget...



	#0 )  smoothness is expected to exist for every texture set you put into this, there cannot be a null in the list for smoothness for each entry of your set.

	#1 )  say you have 100 files with Metallic, AO, Smoothness .. and you have no Detail.  you can leave detail with 0 entries in the list, tool will ignore them its okay.

	#2 )  say you have 10 files with M,Ao,S  .. 10 files M,S .. 10 files with Ao,S  =  in this case do different batches... OR :
			OR : you must add a entry for each index, it can be a null entry in the list... you want all 4 lists to have same # of index .. or totally null list as said above ^^^
	
	#3 ) so yeah.. like .. index 5 will turn into a Mask file .. so whatever is at list index 5 for each of the 4 lists will be put into 1 file ... etc etc each index. 
		and a whole list can be null .. but not smoothness.. we expect smoothness or roughness to always exist, its 2023 its basic like albedo, normal, rough/smooth
		your roughness can turn into the required smoothness file .. but all the ones will be batch inverted if you invert it.. or not invert .. no mix and match... feel free to edit the code!

	#4 ) theres probably other stuff i forgot oops whatever figure it out lol you are smart i believe in you!




=== YOUR FILES AND MASK TO A MATERIAL WITH BETTER LIT SHADER ===		== == == == == == == == == == == == == == ==


	MAKE FOLDER NAMED "Mats" inside the Inputs files folder... the folder with your files you gonna input in this thing and batch...



	1. ) fill all the indexes with matching set or something... maybe the rules from apply here too i forget.. its basically like that.



	2. ) oh and normalmaps in the new materials are like Linear RGBa something or other... its annoying.. i tried to fix it idk whats the deal.. whatever..
		thing is:
	after running the batch you can select all the output mats and click the Fix button at the normal maps place and it will fix it.


== == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == == 

k imma upload this maybe it helps someone it took me like a whole night to figure this out so like have fun good luck 


	

