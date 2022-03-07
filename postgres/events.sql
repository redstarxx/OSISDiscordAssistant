-- Table: public.events
-- This table is to be used for the Events Manager feature (this includes the time to event reminder and the proposal submission reminder).

CREATE TABLE IF NOT EXISTS public.events
(
    id integer NOT NULL,
    event_name character varying(50) COLLATE pg_catalog."default",
    person_in_charge character varying(100) COLLATE pg_catalog."default",
    event_date_unix_timestamp integer,
    next_scheduled_reminder_timestamp integer,
    event_description character varying(255) COLLATE pg_catalog."default",
    executed_reminder_level integer,
    proposal_reminded boolean NOT NULL,
    expired boolean NOT NULL,
    proposal_file_title character varying COLLATE pg_catalog."default",
    proposal_file_content bytea,
    CONSTRAINT events_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.events
    OWNER to postgres;

CREATE SEQUENCE public.events_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- Change table owner accordingly

ALTER TABLE public.events_id_seq OWNER TO postgres;

ALTER SEQUENCE public.events_id_seq OWNED BY public.events.id;

ALTER TABLE ONLY public.events ALTER COLUMN id SET DEFAULT nextval('public.events_id_seq'::regclass);