UPDATE `track`
set artistId = NULL
where id in (select t.id
from `track` t
where artistId IS NOT NULL
and artistId NOT IN (select id from `artist`));

ALTER TABLE `track` 
	ADD CONSTRAINT `track_artist_ibfk_1` 
	FOREIGN KEY (`artistId`) 
	REFERENCES `artist` (`id`) 
	ON DELETE SET NULL; 
	