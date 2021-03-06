declare
   @repeatPreventionDuration int = null,
   @maxPersonCount INT = 6,
   @campusId int = null,
   @order int = 0,
   @maxAlertCount int = 6,
   @alertDateTime datetime,
   @alertCounter int = 0,
   @amount decimal(18,2),
   @amountCurrentMedian decimal(18,2),
   @amountCurrentIqr decimal(18,2),
   @amountIqrMultiplier decimal(6,1),
   @frequencyCurrentMean decimal(6,1),
   @frequencyCurrentStandardDeviation decimal(6,1),
   @frequencyDifferenceFromMean decimal(6,1),
   @frequencyZScore decimal(6,1),
   @personAliasId INT,
   @alertTypeId INT,
   @daysBack INT = 90


begin

   INSERT INTO [FinancialTransactionAlertType]([Name], [CampusId], [AlertType], [ContinueIfMatched], [RepeatPreventionDuration], [FrequencySensitivityScale], [AmountSensitivityScale], [MinimumGiftAmount], [MaximumGiftAmount], [MinimumMedianGiftAmount], [MaximumMedianGiftAmount], [SendBusEvent], [Guid],[Order])
     VALUES
      (N'Large Gift', @campusId, 0, 1, @repeatPreventionDuration, CAST(3.0 AS Decimal(6, 1)), CAST(2.00 AS Decimal(6, 2)), CAST(1.00 AS Decimal(18, 2)), CAST(199.00 AS Decimal(18, 2)), CAST(1.00 AS Decimal(18, 2)), CAST(199.00 AS Decimal(18, 2)), 0, N'e83a842e-b904-4cf3-afbe-83eec02fffb9', @order),
      (N'Late Gift', @campusId, 1, 0, @repeatPreventionDuration, CAST(2.0 AS Decimal(6, 1)), CAST(2.00 AS Decimal(6, 2)), NULL, CAST(154.00 AS Decimal(18, 2)), NULL, CAST(154.00 AS Decimal(18, 2)), 0, N'a21f5038-63b4-4936-a21f-d953b94843fe', @order),
      (N'Spec Campus B', @campusId, 0, 0, @repeatPreventionDuration, CAST(3.0 AS Decimal(6, 1)), CAST(3.00 AS Decimal(6, 2)), NULL, CAST(315.00 AS Decimal(18, 2)), NULL, CAST(315.00 AS Decimal(18, 2)), 0, N'bef1b43b-a3d8-490f-be4f-9b452f7d0865', @order),
	  (N'Test', @campusId, 0, 1, @repeatPreventionDuration, CAST(3.0 AS Decimal(6, 1)), CAST(3.00 AS Decimal(6, 2)), CAST(50.56 AS Decimal(18, 2)), CAST(172.56 AS Decimal(18, 2)), CAST(50.56 AS Decimal(18, 2)), CAST(172.56 AS Decimal(18, 2)), 0, N'66213ce5-bdb3-4405-a8de-65d69aa9a81e', @order),
	  (N'Default', @campusId, 0, 0, @repeatPreventionDuration, CAST(2.0 AS Decimal(6, 1)), CAST(3.00 AS Decimal(6, 2)), CAST(23.10 AS Decimal(18, 2)), CAST(161.10 AS Decimal(18, 2)), CAST(23.10 AS Decimal(18, 2)), CAST(161.10 AS Decimal(18, 2)), 0, N'a3317544-6d17-480a-b0b1-15d13058d3c7', @order)

	  IF CURSOR_STATUS('global','personAliasIdCursor')>=-1
BEGIN
    DEALLOCATE personAliasIdCursor;
END

-- put all personIds in randomly ordered cursor to speed up getting a random personAliasId for each attendance
declare personAliasIdCursor cursor LOCAL FAST_FORWARD for select top( @maxPersonCount ) Id from [PersonAlias] order by newid();
open personAliasIdCursor;

IF CURSOR_STATUS('global','alertTypeIdCursor')>=-1
BEGIN
    DEALLOCATE alertTypeIdCursor;
END

declare alertTypeIdCursor cursor LOCAL FAST_FORWARD for select Id from [FinancialTransactionAlertType] order by newid();
open alertTypeIdCursor;

while @alertCounter < @maxAlertCount
begin

fetch next from personAliasIdCursor into @personAliasId;
begin transaction
	set @alertDateTime = DATEADD(DAY, -@daysBack, SYSDATETIME())
    if (@@FETCH_STATUS != 0) begin
        close personAliasIdCursor;
        open personAliasIdCursor;
        fetch next from personAliasIdCursor into @personAliasId;
    end

	fetch next from alertTypeIdCursor into @alertTypeId;
    if (@@FETCH_STATUS != 0) begin
	   close alertTypeIdCursor;
       open alertTypeIdCursor;
       fetch next from alertTypeIdCursor into @alertTypeId;
    end

	set @amount = ROUND(rand() * 5000, 2);
	set @amountCurrentMedian = ROUND(rand() * 500, 2);
	set @amountCurrentIqr = ROUND(rand() * 200, 2);
	set @amountIqrMultiplier = ROUND(rand() * 100, 2);
	set @frequencyCurrentMean = ROUND(rand() * 100, 1);
	set @frequencyCurrentStandardDeviation = ROUND(rand() * 10, 1);
	set @frequencyDifferenceFromMean = ROUND(rand() * 10, 1);
	set @frequencyZScore = ROUND(rand() * 10, 1);
	INSERT INTO FinancialTransactionAlert
    ([TransactionId], [PersonAliasId], [AlertTypeId], [Amount], [AmountCurrentMedian], [AmountCurrentIqr], [AmountIqrMultiplier], [FrequencyCurrentMean], [FrequencyCurrentStandardDeviation], [FrequencyDifferenceFromMean], [FrequencyZScore], 
                         [AlertDateTime], [AlertDateKey], [Guid])
	VALUES      
	(null ,@personAliasId,@alertTypeId,@amount,@amountCurrentMedian,@amountCurrentIqr,@amountIqrMultiplier,@frequencyCurrentMean,@frequencyCurrentStandardDeviation,@frequencyDifferenceFromMean,@frequencyZScore,@alertDateTime,CONVERT(INT, (CONVERT(CHAR(8), @alertDateTime, 112))),NEWID())

	set @alertCounter += 1;
	set @alertDateTime = DATEADD(dd, 1, @alertDateTime);
commit transaction
end
close alertTypeIdCursor;
close personAliasIdCursor;
end