import { useState } from 'react';

const estadoPlataforma = () => ({
  midiaFisica: false,
  acompanhar: false,
  status: '',
  dataStatus: '',
  tempoEmHoras: '',
  conquistasPossiveis: '',
  conquistasObtidas: ''
});

const numeroOpcional = (valor) => valor === '' ? undefined : Number(valor);

function LibraryGameForm({ platforms, saving, onSubmit }) {
  const [titulo, setTitulo] = useState('');
  const [anoLancamento, setAnoLancamento] = useState('');
  const [selecionadas, setSelecionadas] = useState({});
  const [erro, setErro] = useState('');

  const alternarPlataforma = (plataforma, marcada) => {
    setSelecionadas((atuais) => {
      if (marcada) return { ...atuais, [plataforma]: estadoPlataforma() };
      const proximas = { ...atuais };
      delete proximas[plataforma];
      return proximas;
    });
    setErro('');
  };

  const atualizarPlataforma = (plataforma, campo, valor) => {
    setSelecionadas((atuais) => ({
      ...atuais,
      [plataforma]: { ...atuais[plataforma], [campo]: valor }
    }));
    setErro('');
  };

  const validar = () => {
    if (!titulo.trim()) return 'Informe o nome do jogo.';
    if (Object.keys(selecionadas).length === 0) return 'Selecione pelo menos uma plataforma.';
    if (anoLancamento !== '' && (Number(anoLancamento) < 1 || Number(anoLancamento) > 9999)) {
      return 'Informe um ano de lançamento válido.';
    }

    for (const dados of Object.values(selecionadas)) {
      if (!dados.acompanhar) continue;
      const horas = numeroOpcional(dados.tempoEmHoras);
      const possiveis = numeroOpcional(dados.conquistasPossiveis);
      const obtidas = numeroOpcional(dados.conquistasObtidas);
      if (horas < 0 || possiveis < 0 || obtidas < 0) return 'Horas e conquistas não podem ser negativas.';
      if (obtidas !== undefined && possiveis === undefined) return 'Informe as conquistas possíveis.';
      if (obtidas > possiveis) return 'Conquistas obtidas não podem superar as possíveis.';
    }

    return '';
  };

  const montarAcompanhamento = (dados) => {
    const acompanhamento = {};
    if (dados.status) acompanhamento.status = dados.status;
    if (dados.dataStatus) acompanhamento.dataStatus = dados.dataStatus;
    if (dados.tempoEmHoras !== '') acompanhamento.tempoEmHoras = Number(dados.tempoEmHoras);
    if (dados.conquistasPossiveis !== '') acompanhamento.conquistasPossiveis = Number(dados.conquistasPossiveis);
    if (dados.conquistasObtidas !== '') acompanhamento.conquistasObtidas = Number(dados.conquistasObtidas);
    return acompanhamento;
  };

  const enviar = async (evento) => {
    evento.preventDefault();
    const mensagemErro = validar();
    if (mensagemErro) {
      setErro(mensagemErro);
      return;
    }

    const payload = {
      titulo: titulo.trim(),
      ...(anoLancamento === '' ? {} : { anoLancamento: Number(anoLancamento) }),
      plataformas: platforms
        .filter((plataforma) => selecionadas[plataforma])
        .map((plataforma) => {
          const dados = selecionadas[plataforma];
          return {
            plataforma,
            midiaFisica: dados.midiaFisica,
            ...(dados.acompanhar ? { acompanhamento: montarAcompanhamento(dados) } : {})
          };
        })
    };

    try {
      await onSubmit(payload);
    } catch {
      return;
    }
    setTitulo('');
    setAnoLancamento('');
    setSelecionadas({});
    setErro('');
  };

  return (
    <form className="library-game-form" onSubmit={enviar}>
      <div className="library-game-main-fields">
        <label htmlFor="titulo">Nome do jogo</label>
        <input
          id="titulo"
          value={titulo}
          onChange={(evento) => setTitulo(evento.target.value)}
          maxLength="200"
          placeholder="Ex.: Graveyard Keeper"
          autoComplete="off"
          disabled={saving}
        />

        <label htmlFor="ano-lancamento">Ano de lançamento (opcional)</label>
        <input
          id="ano-lancamento"
          type="number"
          min="1"
          max="9999"
          value={anoLancamento}
          onChange={(evento) => setAnoLancamento(evento.target.value)}
          disabled={saving}
        />
      </div>

      <fieldset className="platform-selector">
        <legend>Plataformas</legend>
        <p>Selecione todos os lugares onde você possui o jogo.</p>
        <div className="platform-options">
          {platforms.map((plataforma) => (
            <label key={plataforma} className={selecionadas[plataforma] ? 'selected' : ''}>
              <input
                type="checkbox"
                aria-label={`Selecionar ${plataforma}`}
                checked={Boolean(selecionadas[plataforma])}
                onChange={(evento) => alternarPlataforma(plataforma, evento.target.checked)}
                disabled={saving}
              />
              <span>{plataforma}</span>
            </label>
          ))}
        </div>
      </fieldset>

      <div className="selected-platforms">
        {platforms.filter((plataforma) => selecionadas[plataforma]).map((plataforma) => {
          const dados = selecionadas[plataforma];
          return (
            <fieldset className="platform-details" key={plataforma}>
              <legend>{plataforma}</legend>
              <div className="platform-toggles">
                <label>
                  <input
                    type="checkbox"
                    aria-label={`Mídia física em ${plataforma}`}
                    checked={dados.midiaFisica}
                    onChange={(evento) => atualizarPlataforma(plataforma, 'midiaFisica', evento.target.checked)}
                    disabled={saving}
                  />
                  Mídia física
                </label>
                <label>
                  <input
                    type="checkbox"
                    aria-label={`Acompanhar progresso em ${plataforma}`}
                    checked={dados.acompanhar}
                    onChange={(evento) => atualizarPlataforma(plataforma, 'acompanhar', evento.target.checked)}
                    disabled={saving}
                  />
                  Acompanhar progresso
                </label>
              </div>

              {dados.acompanhar && (
                <div className="tracking-fields">
                  <label>
                    Status em {plataforma}
                    <select
                      aria-label={`Status em ${plataforma}`}
                      value={dados.status}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'status', evento.target.value)}
                      disabled={saving}
                    >
                      <option value="">Não informado</option>
                      <option value="NaoTerminado">Em andamento</option>
                      <option value="Zerado">Zerado</option>
                      <option value="Dropado">Dropado</option>
                    </select>
                  </label>
                  <label>
                    Data do status em {plataforma}
                    <input
                      aria-label={`Data do status em ${plataforma}`}
                      type="date"
                      value={dados.dataStatus}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'dataStatus', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Horas jogadas em {plataforma}
                    <input
                      aria-label={`Horas jogadas em ${plataforma}`}
                      type="number"
                      min="0"
                      step="0.1"
                      value={dados.tempoEmHoras}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'tempoEmHoras', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Conquistas possíveis em {plataforma}
                    <input
                      aria-label={`Conquistas possíveis em ${plataforma}`}
                      type="number"
                      min="0"
                      value={dados.conquistasPossiveis}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'conquistasPossiveis', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Conquistas obtidas em {plataforma}
                    <input
                      aria-label={`Conquistas obtidas em ${plataforma}`}
                      type="number"
                      min="0"
                      value={dados.conquistasObtidas}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'conquistasObtidas', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                </div>
              )}
            </fieldset>
          );
        })}
      </div>

      {erro && <div className="feedback erro" role="alert">{erro}</div>}
      <button type="submit" disabled={saving}>{saving ? 'Adicionando...' : 'Adicionar à biblioteca'}</button>
    </form>
  );
}

export default LibraryGameForm;
