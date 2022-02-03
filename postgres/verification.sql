-- Table: public.verification
-- Used to store user data temporarily for the main guild verification system.

CREATE TABLE IF NOT EXISTS public.verification
(
    id integer NOT NULL,
    user_id bigint,
    verification_embed_id bigint,
    requested_nickname character varying COLLATE pg_catalog."default",
    CONSTRAINT verification_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.verification
    OWNER to postgres;

CREATE SEQUENCE public.verification_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE public.verification_id_seq OWNER TO postgres;

ALTER SEQUENCE public.verification_id_seq OWNED BY public.verification.id;

ALTER TABLE ONLY public.verification ALTER COLUMN id SET DEFAULT nextval('public.verification_id_seq'::regclass);