DELETE FROM `userrole`;

INSERT INTO `userrole` (isLocked,status,roadieId,createdDate,lastUpdated,name,description,ConcurrencyStamp,NormalizedName) VALUES 
(1,1,'0f3ff165-7b4a-468d-b6ea-35180d8afc4b',UTC_DATE(),NULL,'Admin','Users with Administrative (full) access','0d325432-9bc9-4329-bc49-e07c7eccadf4','ADMIN')
,(1,1,'c2443173-8653-4c86-ae74-92799380c5eb',UTC_DATE(),NULL,'Editor','Users who have Edit Permissions','0d325432-9bc9-4329-bc49-e07c7eccadf4','EDITOR')
;