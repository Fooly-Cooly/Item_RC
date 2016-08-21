//-----------------------------------------------------------------------------
// Item_RC
// Fooly Cooly - No clue when I first made this add-on 10/14/2014
// Aloshi - Provided model
//-----------------------------------------------------------------------------

$RC::Range		= 1250;	//Signal Range
$RC::ShowName	= true;	//Display Current RCer or not
$RC::ShowRange	= true;	//Display Signal Strength
$RC::SignalType	= 0;	//0 is Percentage, 1 is Bars
$RC::BarNum		= 4;	//Number of bars displayed for signal strength
$RC::Hijacking	= false;//Lets people steal vehicles using rc

datablock ItemData(RCItem)
{
	category			= "Item";
	image				= RCImage;
	shapeFile			= "./content/RC.dts";
	iconName			= "./content/icon_RC";
	uiName				= "RC Controller";
	colorShiftColor		= "0.5 0.5 0.5 1";
	doColorShift		= true;
	canDrop				= true;
	emap				= true;
};

datablock ShapeBaseImageData(RCImage)
{
	className		= "ItemImage";
	shapeFile		= "./content/RC.dts";
	mountPoint		= 0;
	eyeOffset		= 0;
	rotation		= "1 0 0 0";
	offset			= "0 0 0";
	item			= RCItem;
	colorShiftColor	= RCItem.colorShiftColor;
	doColorShift	= true;
	armReady		= true;
	melee			= true;
	emap			= true;
	
	stateName[0]                    = "Activate";
	stateTimeoutValue[0]            = 0.5;
	stateTransitionOnTimeout[0]     = "Ready";

	stateName[1]                    = "Ready";
	stateTransitionOnTriggerDown[1] = "Fire";
	stateAllowImageChange[1]        = true;

	stateName[2]                    = "Fire";
	stateTransitionOnTriggerUp[2]	= "Ready";
	stateScript[2]                  = "onFire";
};

function RCImage::onFire(%this, %pl, %slot)
{
	%cl  = %pl.client;
	%eye = %pl.getEyePoint();
	%vec = %pl.getEyeVector();
	%end = vectorAdd(%eye,vectorScale(%vec, 10));
	%veh = ContainerRayCast(%eye, %end, $TypeMasks::VehicleObjectType | $TypeMasks::PlayerObjectType, %pl);
	if(!%veh || %veh.player || %veh == %pl.getObjectMount())
		centerPrint(%cl, "You must look at a vehicle.", 1);
	else if(!miniGameCanUse(%pl, %veh))
		centerPrint(%cl, $lastError, 3);
	else if(getTrustLevel(%pl, %veh) < 2)
		centerPrint(%cl, $lastError, 3);
	else if(%veh.getControllingObject() && !$RC::Hijacking)
		centerPrint(%cl, "That is already being driven.", 1);
	else
	{
		if($RC::ShowName)
			%veh.setShapeName(%cl.name @ "'s RC");
		%pl.RC = %veh;
		%veh.lastDrivingClient = %cl;
		%pl.setControlObject(%veh);
		%pl.rcDistance(%veh);
		%veh.isRC = 1;
		centerPrint(%cl, "Your RC Controller is now on.", 2);
	}
}

function Player::rcDistance(%this, %veh)
{
	if(!isObject(%this.RC))
		return serverCmdTurnOffRC(%this.client);
	%len = vectorSub(%this.getPosition(), %veh.getPosition());
	%len = (vectorLen(%len) / 0.5) - 1;
	if(%len > $RC::Range)
		return serverCmdTurnOffRC(%this.client);
	if($RC::ShowRange)
		switch($RC::SignalType)
		{
			case 0:
				%len = 100 - ((%len * 100) / $RC::Range);
				%len = getSubStr(%len, 0, strpos(%len, ".")) @ "%";
				bottomPrint(%this.client, "Signal:" SPC %len, 1, 2);
			case 1:
				%len = $RC::BarNum - ((%len * $RC::BarNum) / $RC::Range);
				for(%i=0;%i<%len;%i++) %bars = %bars@"|";
				bottomPrint(%this.client, "Signal:" SPC %bars, 1, 2);
		}
	%this.schedule(500, "rcDistance", %veh);
}

function serverCmdTurnOffRC(%cl)
{
	if(isObject((%pl = %cl.player)))
	{
		%pl.setControlObject(%pl);
		bottomPrint(%cl, "Your RC Controller is now off.", 2);
	}
	if(isObject((%veh = %pl.RC)))
	{
		%veh.setVelocity("0 0 0");
		%veh.setShapeName("");
		%veh.isRC = 0;
		%pl.RC = 0;
	}
}

package RcController
{
	function Player::setControlObject(%this, %obj)
	{
		if(!%obj.isRC)
			parent::setControlObject(%this, %obj);
	}
	
	function WheeledVehicleData::onTrigger(%this, %obj, %trig, %val)
	{
		if(%val && %trig == 4 && %obj.isRC)
			serverCmdTurnOffRc(%obj.getControllingClient());
		Parent::onTrigger(%this, %obj, %trig, %val);
	}

	function FlyingVehicleData::onTrigger(%this, %obj, %trig, %val)
	{
		if(%val && %trig == 4 && %obj.isRC)
			serverCmdTurnOffRc(%obj.getControllingClient());
		Parent::onTrigger(%this, %obj, %trig, %val);
	}
};
ActivatePackage(RcController);