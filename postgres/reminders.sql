-- Table: public.reminders

-- DROP TABLE IF EXISTS public.reminders;

CREATE TABLE IF NOT EXISTS public.reminders
(
    id integer NOT NULL,
    initiating_user_id bigint,
    targeted_user_or_role_mention character varying(25) COLLATE pg_catalog."default",
    unix_timestamp_remind_time integer,
    target_guild_id bigint,
    reply_message_id bigint,
    target_channel_id bigint,
    cancelled boolean NOT NULL,
    content character varying(2000) COLLATE pg_catalog."default",
    CONSTRAINT reminders_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.reminders
    OWNER to postgres;

ALTER TABLE IF EXISTS public.reminders
    OWNER to postgres;

CREATE SEQUENCE public.reminders_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE public.reminders_id_seq OWNER TO postgres;

ALTER SEQUENCE public.reminders_id_seq OWNED BY public.reminders.id;

ALTER TABLE ONLY public.reminders ALTER COLUMN id SET DEFAULT nextval('public.reminders_id_seq'::regclass);